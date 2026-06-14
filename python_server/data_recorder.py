# -*- coding: utf-8 -*-
"""
数据记录与分析模块
负责记录交互事件、统计分析和离线缓存
Python 3.11 兼容
"""

import json
import os
import pickle
import time
from dataclasses import dataclass, field, asdict
from datetime import datetime
from typing import Dict, Any, List, Optional
from enum import Enum

from config import DATA_CONFIG, EVENT_TYPES


class EventCorrectness(Enum):
    """事件正确性枚举"""
    CORRECT = "correct"
    WRONG = "wrong"
    UNKNOWN = "unknown"


@dataclass
class InteractionEvent:
    """交互事件数据类"""
    event_id: str
    event_type: str
    data: Dict[str, Any]
    timestamp: float
    session_id: str
    context: str
    correctness: EventCorrectness = EventCorrectness.UNKNOWN
    
    def to_dict(self) -> Dict[str, Any]:
        """转换为字典"""
        result = asdict(self)
        result["correctness"] = self.correctness.value
        return result
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "InteractionEvent":
        """从字典创建实例"""
        data["correctness"] = EventCorrectness(data.get("correctness", "unknown"))
        return cls(**data)


@dataclass
class SessionStats:
    """会话统计数据类"""
    session_id: str
    start_time: float
    end_time: Optional[float] = None
    total_events: int = 0
    correct_events: int = 0
    wrong_events: int = 0
    consecutive_correct: int = 0
    max_consecutive_correct: int = 0
    total_duration: float = 0.0
    scene_stats: Dict[str, Dict[str, int]] = field(default_factory=dict)
    
    @property
    def accuracy(self) -> float:
        """计算正确率"""
        if self.correct_events + self.wrong_events == 0:
            return 0.0
        return self.correct_events / (self.correct_events + self.wrong_events)
    
    def to_dict(self) -> Dict[str, Any]:
        """转换为字典"""
        result = asdict(self)
        result["accuracy"] = self.accuracy
        return result
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "SessionStats":
        """从字典创建实例"""
        data_copy = {k: v for k, v in data.items() if k != "accuracy"}
        return cls(**data_copy)


class DataRecorder:
    """数据记录器类，负责事件记录、统计分析和离线缓存"""
    
    def __init__(self, session_id: Optional[str] = None):
        """
        初始化数据记录器
        
        参数:
            session_id: 会话ID，不提供则自动生成
        """
        self.session_id = session_id or self._generate_session_id()
        self.events: List[InteractionEvent] = []
        self.stats = SessionStats(
            session_id=self.session_id,
            start_time=time.time()
        )
        
        self.cache_format = DATA_CONFIG.get("cache_format", "json")
        self.streak_threshold = DATA_CONFIG.get("streak_threshold", 3)
        
        self._ensure_directories()
        self._load_cached_data()

    @staticmethod
    def _generate_session_id() -> str:
        """生成会话ID"""
        return f"session_{datetime.now().strftime('%Y%m%d_%H%M%S')}"

    def _ensure_directories(self):
        """确保必要的目录存在"""
        directories = [
            DATA_CONFIG["cache_dir"],
            DATA_CONFIG["log_dir"],
            DATA_CONFIG["report_dir"],
        ]
        
        for directory in directories:
            if not os.path.exists(directory):
                os.makedirs(directory, exist_ok=True)

    def _load_cached_data(self):
        """加载缓存数据"""
        cache_file = self._get_cache_file_path()
        
        if not os.path.exists(cache_file):
            return
        
        try:
            if self.cache_format == "json":
                with open(cache_file, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    self._restore_from_cache(data)
            else:
                with open(cache_file, "rb") as f:
                    data = pickle.load(f)
                    self._restore_from_cache(data)
        except (json.JSONDecodeError, pickle.UnpicklingError, OSError) as e:
            print(f"加载缓存数据失败: {e}")

    def _restore_from_cache(self, data: Dict[str, Any]):
        """从缓存数据恢复"""
        if "events" in data:
            self.events = [InteractionEvent.from_dict(evt) for evt in data["events"]]
        if "stats" in data:
            self.stats = SessionStats.from_dict(data["stats"])

    def _get_cache_file_path(self) -> str:
        """获取缓存文件路径"""
        extension = "json" if self.cache_format == "json" else "pkl"
        return os.path.join(DATA_CONFIG["cache_dir"], f"{self.session_id}.{extension}")

    def _get_log_file_path(self) -> str:
        """获取日志文件路径"""
        date_str = datetime.now().strftime("%Y%m%d")
        return os.path.join(DATA_CONFIG["log_dir"], f"events_{date_str}.log")

    def log_event(self, event_type: str, data: Dict[str, Any], 
                  context: str = "unknown") -> InteractionEvent:
        """
        记录交互事件，并自动判断正确性
        
        参数:
            event_type: 事件类型
            data: 事件数据
            context: 上下文场景
            
        返回:
            记录的事件对象
        """
        event_id = f"evt_{int(time.time() * 1000)}_{len(self.events)}"
        timestamp = time.time()
        
        correctness = self._determine_correctness(event_type, data)
        
        event = InteractionEvent(
            event_id=event_id,
            event_type=event_type,
            data=data,
            timestamp=timestamp,
            session_id=self.session_id,
            context=context,
            correctness=correctness
        )
        
        self.events.append(event)
        self._update_stats(event)
        self._save_to_cache()
        self._append_to_log(event)
        
        return event

    def _determine_correctness(self, event_type: str, 
                                data: Dict[str, Any]) -> EventCorrectness:
        """
        根据事件类型和数据判断正确性
        
        参数:
            event_type: 事件类型
            data: 事件数据
            
        返回:
            事件正确性
        """
        if "is_correct" in data:
            if data["is_correct"]:
                return EventCorrectness.CORRECT
            else:
                return EventCorrectness.WRONG
        
        if event_type == "intent_detected":
            confidence = data.get("confidence", 0)
            if confidence >= 0.7:
                return EventCorrectness.CORRECT
            elif confidence >= 0.4:
                return EventCorrectness.UNKNOWN
        
        if event_type == "user_action":
            action = data.get("action", "")
            expected = data.get("expected_action", "")
            if action == expected:
                return EventCorrectness.CORRECT
            elif expected:
                return EventCorrectness.WRONG
        
        if event_type == "color_selection":
            selected = data.get("selected_color", "")
            expected = data.get("expected_color", "")
            if selected and expected:
                if selected == expected:
                    return EventCorrectness.CORRECT
                else:
                    return EventCorrectness.WRONG
        
        if event_type == "item_selection":
            selected = data.get("selected_item", "")
            expected = data.get("expected_item", "")
            if selected and expected:
                if selected == expected:
                    return EventCorrectness.CORRECT
                else:
                    return EventCorrectness.WRONG
        
        return EventCorrectness.UNKNOWN

    def _update_stats(self, event: InteractionEvent):
        """更新统计数据"""
        self.stats.total_events += 1
        
        if event.correctness == EventCorrectness.CORRECT:
            self.stats.correct_events += 1
            self.stats.consecutive_correct += 1
            self.stats.max_consecutive_correct = max(
                self.stats.max_consecutive_correct, 
                self.stats.consecutive_correct
            )
        elif event.correctness == EventCorrectness.WRONG:
            self.stats.wrong_events += 1
            self.stats.consecutive_correct = 0
        
        if event.context not in self.stats.scene_stats:
            self.stats.scene_stats[event.context] = {
                "correct": 0,
                "wrong": 0,
                "total": 0
            }
        
        self.stats.scene_stats[event.context]["total"] += 1
        if event.correctness == EventCorrectness.CORRECT:
            self.stats.scene_stats[event.context]["correct"] += 1
        elif event.correctness == EventCorrectness.WRONG:
            self.stats.scene_stats[event.context]["wrong"] += 1
        
        self.stats.total_duration = time.time() - self.stats.start_time

    def _save_to_cache(self):
        """保存到离线缓存"""
        cache_file = self._get_cache_file_path()
        
        cache_data = {
            "session_id": self.session_id,
            "events": [evt.to_dict() for evt in self.events],
            "stats": self.stats.to_dict(),
            "last_updated": time.time()
        }
        
        try:
            if self.cache_format == "json":
                with open(cache_file, "w", encoding="utf-8") as f:
                    json.dump(cache_data, f, ensure_ascii=False, indent=2)
            else:
                with open(cache_file, "wb") as f:
                    pickle.dump(cache_data, f)
        except OSError as e:
            print(f"保存缓存失败: {e}")

    def _append_to_log(self, event: InteractionEvent):
        """追加到日志文件"""
        log_file = self._get_log_file_path()
        log_entry = {
            "timestamp": datetime.fromtimestamp(event.timestamp).isoformat(),
            "event_id": event.event_id,
            "event_type": event.event_type,
            "session_id": event.session_id,
            "context": event.context,
            "correctness": event.correctness.value,
            "data": event.data
        }
        
        try:
            with open(log_file, "a", encoding="utf-8") as f:
                f.write(json.dumps(log_entry, ensure_ascii=False) + "\n")
        except OSError as e:
            print(f"写入日志失败: {e}")

    def get_current_stats(self) -> Dict[str, Any]:
        """
        获取当前统计数据
        
        返回:
            包含正确率、总时长、连续正确次数等的字典
        """
        self.stats.total_duration = time.time() - self.stats.start_time
        
        return {
            "session_id": self.session_id,
            "accuracy": round(self.stats.accuracy, 2),
            "total_duration": round(self.stats.total_duration, 1),
            "total_events": self.stats.total_events,
            "correct_events": self.stats.correct_events,
            "wrong_events": self.stats.wrong_events,
            "consecutive_correct": self.stats.consecutive_correct,
            "max_consecutive_correct": self.stats.max_consecutive_correct,
            "should_trigger_feedback": self.stats.consecutive_correct >= self.streak_threshold,
            "scene_stats": self.stats.scene_stats
        }

    def end_session(self) -> SessionStats:
        """
        结束当前会话
        
        返回:
            最终的会话统计数据
        """
        self.stats.end_time = time.time()
        self.stats.total_duration = self.stats.end_time - self.stats.start_time
        
        self._save_to_cache()
        
        return self.stats

    def get_events(self, event_type: Optional[str] = None,
                   context: Optional[str] = None) -> List[InteractionEvent]:
        """
        获取事件列表，可按类型或上下文过滤
        
        参数:
            event_type: 事件类型过滤
            context: 上下文过滤
            
        返回:
            符合条件的事件列表
        """
        filtered_events = self.events
        
        if event_type:
            filtered_events = [
                evt for evt in filtered_events if evt.event_type == event_type
            ]
        
        if context:
            filtered_events = [
                evt for evt in filtered_events if evt.context == context
            ]
        
        return filtered_events

    def get_events_by_correctness(self, correctness: EventCorrectness
                                  ) -> List[InteractionEvent]:
        """
        按正确性获取事件列表
        
        参数:
            correctness: 正确性枚举值
            
        返回:
            符合条件的事件列表
        """
        return [evt for evt in self.events if evt.correctness == correctness]

    def clear_cache(self):
        """清除当前会话的缓存"""
        cache_file = self._get_cache_file_path()
        if os.path.exists(cache_file):
            os.remove(cache_file)

    @classmethod
    def get_available_sessions(cls) -> List[str]:
        """
        获取所有可用的会话ID
        
        返回:
            会话ID列表
        """
        cache_dir = DATA_CONFIG["cache_dir"]
        if not os.path.exists(cache_dir):
            return []
        
        sessions = []
        for file_name in os.listdir(cache_dir):
            if file_name.startswith("session_"):
                session_id = file_name.rsplit(".", 1)[0]
                sessions.append(session_id)
        
        return sorted(sessions, reverse=True)

    @classmethod
    def load_session(cls, session_id: str) -> Optional["DataRecorder"]:
        """
        加载指定会话的数据
        
        参数:
            session_id: 会话ID
            
        返回:
            DataRecorder实例或None
        """
        recorder = cls(session_id=session_id)
        if recorder.events or recorder.stats.total_events > 0:
            return recorder
        return None
