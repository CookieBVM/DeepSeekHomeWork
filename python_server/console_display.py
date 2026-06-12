# -*- coding: utf-8 -*-
"""
实时控制台统计显示模块
当Unity发送数据到Python时，在Python终端显示实时统计数据
Python 3.11 兼容
"""

import os
import sys
import time
from typing import Dict, Any, List, Optional
from datetime import datetime

from utf8_output import safe_print


class ConsoleStatsDisplay:
    """控制台统计显示类"""

    def __init__(self):
        """初始化"""
        self.session_started = False
        self.session_id = None
        self.start_time = None
        self.total_events = 0
        self.correct_events = 0
        self.wrong_events = 0
        self.consecutive_correct = 0
        self.max_consecutive_correct = 0
        self.module_stats = {}

    def print_welcome(self) -> None:
        """打印欢迎信息"""
        safe_print("")
        safe_print("╔══════════════════════════════════════════════════════════════════╗")
        safe_print("║                                                                  ║")
        safe_print("║            🎮 Python 实时统计监视器已启动                          ║")
        safe_print("║                                                                  ║")
        safe_print("╚══════════════════════════════════════════════════════════════════╝")
        safe_print("")
        safe_print("[提示] 等待 Unity 连接...")
        safe_print("[提示] 当 Unity 中开始游戏时，统计数据将实时显示在这里")
        safe_print("")

    def handle_session_started(self, data: Dict[str, Any]) -> None:
        """
        处理会话开始事件

        参数:
            data: 会话数据
                - session_id: 会话ID
                - module: 模块名称
                - scenario: 场景
                - difficulty: 难度
        """
        self.session_started = True
        self.session_id = data.get("session_id", "unknown")
        self.start_time = time.time()
        self.total_events = 0
        self.correct_events = 0
        self.wrong_events = 0
        self.consecutive_correct = 0
        self.max_consecutive_correct = 0
        self.module_stats = {}

        module_name = data.get("module", "未知模块")
        scenario = data.get("scenario", "未知场景")
        difficulty = data.get("difficulty", "未知难度")

        safe_print("")
        safe_print("═" * 70)
        safe_print("                    🎮 新会话开始")
        safe_print("═" * 70)
        safe_print(f"  会话ID: {self.session_id}")
        safe_print(f"  开始时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        safe_print(f"  模块: {module_name}")
        safe_print(f"  场景: {scenario}")
        safe_print(f"  难度: {difficulty}")
        safe_print("═" * 70)
        safe_print("  当前统计:")
        safe_print("    总交互数: 0")
        safe_print("    正确次数: 0")
        safe_print("    错误次数: 0")
        safe_print("    正确率: 0%")
        safe_print("    连续正确: 0")
        safe_print("    最高连续正确: 0")
        safe_print("═" * 70)
        safe_print("")

    def handle_interaction(self, data: Dict[str, Any], stats: Dict[str, Any]) -> None:
        """
        处理交互事件

        参数:
            data: 事件数据
            stats: 统计数据
        """
        self.total_events += 1

        event_type = data.get("event_type", "unknown")

        if event_type == "session_started":
            self.handle_session_started(data)
            return

        input_content = ""
        response = ""
        is_correct = None
        participant = data.get("participant", "")
        input_mode = data.get("input_mode", "")

        if "input" in data:
            input_content = data["input"]
        if "response" in data:
            response = data["response"]
        if "is_correct" in data:
            is_correct = data["is_correct"]

        if is_correct is not None:
            if is_correct:
                self.correct_events += 1
                self.consecutive_correct += 1
                self.max_consecutive_correct = max(
                    self.max_consecutive_correct,
                    self.consecutive_correct
                )
            else:
                self.wrong_events += 1
                self.consecutive_correct = 0

        accuracy = 0.0
        if self.correct_events + self.wrong_events > 0:
            accuracy = self.correct_events / (self.correct_events + self.wrong_events)

        safe_print("-" * 70)
        safe_print(f"📝 新交互记录 [{datetime.now().strftime('%H:%M:%S')}]")
        safe_print("-" * 70)

        if participant:
            safe_print(f"  参与者: {participant}")
        if input_mode:
            safe_print(f"  输入方式: {input_mode}")
        if input_content:
            safe_print(f"  输入内容: \"{input_content}\"")
        if response:
            safe_print(f"  数字人回复: \"{response}\"")

        if is_correct is not None:
            result_str = "✅ 正确" if is_correct else "❌ 错误"
            safe_print(f"  结果: {result_str}")
        else:
            safe_print(f"  结果: ⚪ 未判定")

        safe_print(f"  当前连续正确: {self.consecutive_correct}")

        if self.consecutive_correct >= 3:
            safe_print(f"  🔥 连胜中! 已连续 {self.consecutive_correct} 次正确")

        safe_print("-" * 70)

        if stats:
            self._update_stats_from_dict(stats)
        self._print_current_stats()

    def _update_stats_from_dict(self, stats: Dict[str, Any]) -> None:
        """从统计数据字典更新"""
        if "correct_events" in stats:
            self.correct_events = stats["correct_events"]
        if "wrong_events" in stats:
            self.wrong_events = stats["wrong_events"]
        if "consecutive_correct" in stats:
            self.consecutive_correct = stats["consecutive_correct"]
        if "max_consecutive_correct" in stats:
            self.max_consecutive_correct = stats["max_consecutive_correct"]

    def _print_current_stats(self) -> None:
        """打印当前统计"""
        total = self.total_events
        correct = self.correct_events
        wrong = self.wrong_events

        accuracy = 0.0
        if correct + wrong > 0:
            accuracy = correct / (correct + wrong)

        accuracy_label = "优秀" if accuracy >= 0.7 else (
            "良好" if accuracy >= 0.4 else "需努力"
        )

        duration = 0.0
        if self.start_time:
            duration = time.time() - self.start_time

        safe_print("")
        safe_print("═" * 70)
        safe_print("                    📊 实时统计数据")
        safe_print("═" * 70)
        safe_print(f"  总交互数: {total}")
        safe_print(f"  正确次数: {correct}")
        safe_print(f"  错误次数: {wrong}")
        safe_print(f"  正确率: {accuracy * 100:.1f}% ({accuracy_label})")
        safe_print(f"  当前连续正确: {self.consecutive_correct}")
        safe_print(f"  最高连续正确: {self.max_consecutive_correct}")
        safe_print(f"  当前时长: {duration:.1f}秒")

        if self.module_stats:
            safe_print("-" * 70)
            safe_print("  各模块表现:")
            for module_name, module_data in self.module_stats.items():
                m_total = module_data["total"]
                m_correct = module_data["correct"]
                m_acc = m_correct / m_total * 100 if m_total > 0 else 0
                safe_print(f"    {module_name}: {m_correct}/{m_total} ({m_acc:.1f}%)")

        if self.consecutive_correct >= 3:
            safe_print("-" * 70)
            safe_print(f"  🔥 连胜中! 连续 {self.consecutive_correct} 次正确!")

        if accuracy >= 0.8 and correct >= 5:
            safe_print("  🌟 表现出色! 继续保持!")

        safe_print("═" * 70)
        safe_print("")

    def handle_stats_updated(self, stats: Dict[str, Any]) -> None:
        """
        处理统计更新事件

        参数:
            stats: 统计数据
        """
        self.total_events = stats.get("total_interactions", self.total_events)
        self.correct_events = stats.get("correct", self.correct_events)
        self.wrong_events = stats.get("wrong", self.wrong_events)
        self.consecutive_correct = stats.get(
            "max_consecutive_correct",
            self.consecutive_correct
        )
        self.max_consecutive_correct = stats.get(
            "max_consecutive_correct",
            self.max_consecutive_correct
        )

        accuracy = stats.get("accuracy", 0.0)
        duration = stats.get("duration_seconds", 0.0)

        accuracy_label = "优秀" if accuracy >= 0.7 else (
            "良好" if accuracy >= 0.4 else "需努力"
        )

        safe_print("")
        safe_print("═" * 70)
        safe_print("                    📊 统计数据更新")
        safe_print("═" * 70)
        safe_print(f"  总交互数: {self.total_events}")
        safe_print(f"  正确次数: {self.correct_events}")
        safe_print(f"  错误次数: {self.wrong_events}")
        safe_print(f"  正确率: {accuracy * 100:.1f}% ({accuracy_label})")
        safe_print(f"  最高连续正确: {self.max_consecutive_correct}")
        safe_print(f"  当前时长: {duration:.1f}秒")
        safe_print("═" * 70)
        safe_print("")

    def handle_session_ended(self, data: Dict[str, Any]) -> None:
        """
        处理会话结束事件

        参数:
            data: 会话数据
        """
        safe_print("")
        safe_print("═" * 70)
        safe_print("                    🎉 会话结束!")
        safe_print("═" * 70)

        total = self.total_events
        correct = self.correct_events
        wrong = self.wrong_events
        max_streak = self.max_consecutive_correct

        accuracy = 0.0
        if correct + wrong > 0:
            accuracy = correct / (correct + wrong)

        accuracy_label = "优秀" if accuracy >= 0.7 else (
            "良好" if accuracy >= 0.4 else "需努力"
        )

        duration = 0.0
        if self.start_time:
            duration = time.time() - self.start_time

        total_minutes = int(duration // 60)
        total_seconds = int(duration % 60)

        safe_print(f"  总正确率: {accuracy * 100:.1f}% ({accuracy_label})")
        safe_print(f"  游戏时长: {total_minutes} 分 {total_seconds} 秒")
        safe_print(f"  正确互动次数: {correct}")
        safe_print(f"  需要改进的互动: {wrong}")
        safe_print(f"  最高连续正确: {max_streak} 次")
        safe_print("═" * 70)

        strengths = self._identify_strengths(accuracy, max_streak, duration)
        if strengths:
            safe_print("  【宝宝的优点】✨")
            for i, strength in enumerate(strengths, 1):
                safe_print(f"    {i}. {strength}")

        areas_to_improve = self._identify_areas_to_improve(accuracy, duration)
        if areas_to_improve:
            safe_print("  【继续加油的方向】💪")
            for i, area in enumerate(areas_to_improve, 1):
                safe_print(f"    {i}. {area}")

        safe_print("═" * 70)
        safe_print("                    宝宝真棒! 继续加油哦! 🌟")
        safe_print("═" * 70)
        safe_print("")
        safe_print("[提示] 等待下一次游戏开始...")
        safe_print("")

        self.session_started = False

    def _identify_strengths(self, accuracy: float, max_streak: int,
                             duration: float) -> List[str]:
        """识别优点"""
        strengths = []

        if accuracy >= 0.7:
            strengths.append("整体表现优秀，能够准确完成互动任务")

        if max_streak >= 5:
            strengths.append("能够持续专注，保持良好的表现状态")

        if duration >= 600:
            strengths.append("能够长时间保持专注力")

        if not strengths:
            strengths.append("正在积极参与游戏互动")

        return strengths

    def _identify_areas_to_improve(self, accuracy: float,
                                    duration: float) -> List[str]:
        """识别需要改进的方向"""
        areas = []

        if accuracy < 0.5:
            areas.append("可以多加练习，提高互动准确率")

        if duration < 180:
            areas.append("可以尝试延长游戏时间，保持更长时间的专注")

        return areas

    def handle_event(self, event_type: str, data: Dict[str, Any],
                     stats: Optional[Dict[str, Any]] = None) -> None:
        """
        通用事件处理入口

        参数:
            event_type: 事件类型
            data: 事件数据
            stats: 统计数据（可选）
        """
        safe_print(f"[调试] 收到事件: {event_type}, data={data}")

        if event_type == "session_started":
            self.handle_session_started(data)
        elif event_type == "stats_updated":
            self.handle_stats_updated(data)
        elif event_type == "session_ended":
            self.handle_session_ended(data)
        else:
            self.handle_interaction(data, stats or {})

    def handle_raw_event_data(self, event_data: Dict[str, Any]) -> None:
        """
        处理原始事件数据（从Unity直接发送的）

        参数:
            event_data: 完整的事件数据字典
                - event_type: 事件类型
                - data: 事件详情
                - stats: 统计数据（可选）
        """
        event_type = event_data.get("event_type", "unknown")
        data = event_data.get("data", {})
        stats = event_data.get("stats", None)

        self.handle_event(event_type, data, stats)


console_display = ConsoleStatsDisplay()
