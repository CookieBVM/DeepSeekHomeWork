# -*- coding: utf-8 -*-
"""
家长端数据同步与报告生成模块
负责生成游戏总结和数据导出
Python 3.11 兼容
"""

import json
import os
from dataclasses import dataclass, asdict
from datetime import datetime
from typing import Dict, Any, List, Optional

from config import DATA_CONFIG, SCENES
from data_recorder import DataRecorder, SessionStats


@dataclass
class ParentReport:
    """家长报告数据类"""
    session_id: str
    report_date: str
    summary_text: str
    accuracy: float
    total_duration: float
    correct_events: int
    wrong_events: int
    max_consecutive_correct: int
    scene_performance: List[Dict[str, Any]]
    strengths: List[str]
    areas_to_improve: List[str]
    
    def to_dict(self) -> Dict[str, Any]:
        """转换为字典"""
        return asdict(self)


class ReportGenerator:
    """报告生成器类，负责生成家长报告和数据导出"""
    
    def __init__(self, data_recorder: Optional[DataRecorder] = None):
        """
        初始化报告生成器
        
        参数:
            data_recorder: DataRecorder实例，用于获取会话数据
        """
        self.data_recorder = data_recorder
        
        self._ensure_report_directory()

    @staticmethod
    def _ensure_report_directory():
        """确保报告目录存在"""
        report_dir = DATA_CONFIG["report_dir"]
        if not os.path.exists(report_dir):
            os.makedirs(report_dir, exist_ok=True)

    def generate_summary(self, stats: Optional[SessionStats] = None) -> str:
        """
        生成自然语言描述的总结
        
        参数:
            stats: 会话统计数据，不提供则使用data_recorder中的数据
            
        返回:
            自然语言总结字符串
        """
        if stats is None:
            if self.data_recorder is None:
                return "暂无数据可生成报告。"
            stats = self.data_recorder.stats
        
        accuracy = stats.accuracy
        correct_count = stats.correct_events
        max_streak = stats.max_consecutive_correct
        duration_minutes = stats.total_duration / 60
        
        summary_parts = []
        
        summary_parts.append(f"宝宝今天在游戏中表现很棒！")
        
        if accuracy >= 0.8:
            summary_parts.append(
                f"正确率达到了 {int(accuracy * 100)}%，"
                f"完成了 {correct_count} 次正确的互动。"
            )
        elif accuracy >= 0.6:
            summary_parts.append(
                f"正确率为 {int(accuracy * 100)}%，"
                f"完成了 {correct_count} 次正确的互动，继续加油！"
            )
        elif accuracy >= 0.4:
            summary_parts.append(
                f"正确率为 {int(accuracy * 100)}%，"
                f"完成了 {correct_count} 次正确互动，多多练习会更好哦！"
            )
        else:
            summary_parts.append(
                f"今天完成了 {correct_count} 次互动，"
                f"让我们一起加油，下次会更好！"
            )
        
        if max_streak >= 5:
            summary_parts.append(f"特别棒！最高连续正确 {max_streak} 次！")
        elif max_streak >= 3:
            summary_parts.append(f"很好！最高连续正确 {max_streak} 次。")
        
        if duration_minutes >= 10:
            summary_parts.append(
                f"今天玩了 {int(duration_minutes)} 分钟，非常专注！"
            )
        elif duration_minutes >= 5:
            summary_parts.append(
                f"今天玩了 {int(duration_minutes)} 分钟，表现不错！"
            )
        else:
            summary_parts.append(
                f"今天玩了 {int(duration_minutes)} 分钟，下次可以多玩一会儿。"
            )
        
        scene_summary = self._generate_scene_summary(stats.scene_stats)
        if scene_summary:
            summary_parts.append(scene_summary)
        
        return "".join(summary_parts)

    def _generate_scene_summary(self, scene_stats: Dict[str, Dict[str, int]]) -> str:
        """
        生成各场景的详细总结
        
        参数:
            scene_stats: 各场景统计数据
            
        返回:
            场景总结字符串
        """
        if not scene_stats:
            return ""
        
        scene_summaries = []
        
        for scene_key, stats in scene_stats.items():
            scene_name = SCENES.get(scene_key, scene_key)
            total = stats.get("total", 0)
            correct = stats.get("correct", 0)
            
            if total == 0:
                continue
            
            accuracy = correct / total
            if accuracy >= 0.7:
                scene_summaries.append(
                    f"在{scene_name}中表现出色，"
                    f"完成了 {correct}/{total} 次正确互动。"
                )
            elif accuracy >= 0.4:
                scene_summaries.append(
                    f"在{scene_name}中完成了 {correct}/{total} 次互动，"
                    f"继续加油！"
                )
            else:
                scene_summaries.append(
                    f"在{scene_name}中完成了 {correct}/{total} 次互动，"
                    f"可以多加练习这个场景。"
                )
        
        return "".join(scene_summaries)

    def generate_report(self, stats: Optional[SessionStats] = None) -> ParentReport:
        """
        生成完整的家长报告
        
        参数:
            stats: 会话统计数据
            
        返回:
            ParentReport对象
        """
        if stats is None:
            if self.data_recorder is None:
                raise ValueError("需要提供SessionStats或初始化时传入DataRecorder")
            stats = self.data_recorder.stats
        
        scene_performance = self._calculate_scene_performance(stats.scene_stats)
        strengths = self._identify_strengths(stats, scene_performance)
        areas_to_improve = self._identify_areas_to_improve(stats, scene_performance)
        
        report = ParentReport(
            session_id=stats.session_id,
            report_date=datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            summary_text=self.generate_summary(stats),
            accuracy=round(stats.accuracy, 2),
            total_duration=round(stats.total_duration, 1),
            correct_events=stats.correct_events,
            wrong_events=stats.wrong_events,
            max_consecutive_correct=stats.max_consecutive_correct,
            scene_performance=scene_performance,
            strengths=strengths,
            areas_to_improve=areas_to_improve
        )
        
        return report

    def _calculate_scene_performance(self, scene_stats: Dict[str, Dict[str, int]]
                                      ) -> List[Dict[str, Any]]:
        """
        计算各场景的表现数据
        
        参数:
            scene_stats: 各场景统计数据
            
        返回:
            场景表现列表
        """
        performance = []
        
        for scene_key, stats in scene_stats.items():
            scene_name = SCENES.get(scene_key, scene_key)
            total = stats.get("total", 0)
            correct = stats.get("correct", 0)
            wrong = stats.get("wrong", 0)
            
            accuracy = correct / total if total > 0 else 0.0
            
            performance.append({
                "scene_key": scene_key,
                "scene_name": scene_name,
                "total_interactions": total,
                "correct_interactions": correct,
                "wrong_interactions": wrong,
                "accuracy": round(accuracy, 2)
            })
        
        return sorted(performance, key=lambda x: x["accuracy"], reverse=True)

    def _identify_strengths(self, stats: SessionStats,
                            scene_performance: List[Dict[str, Any]]
                            ) -> List[str]:
        """
        识别孩子的优势领域
        
        参数:
            stats: 会话统计数据
            scene_performance: 场景表现列表
            
        返回:
            优势列表
        """
        strengths = []
        
        if stats.accuracy >= 0.7:
            strengths.append("整体表现优秀，能够准确完成互动任务")
        
        if stats.max_consecutive_correct >= 5:
            strengths.append("能够持续专注，保持良好的表现状态")
        
        for scene in scene_performance:
            if scene["accuracy"] >= 0.8 and scene["total_interactions"] >= 3:
                strengths.append(f"在{scene['scene_name']}中表现特别出色")
        
        if stats.total_duration >= 600:
            strengths.append("能够长时间保持专注力")
        
        if not strengths:
            strengths.append("正在积极参与游戏互动")
        
        return strengths

    def _identify_areas_to_improve(self, stats: SessionStats,
                                    scene_performance: List[Dict[str, Any]]
                                    ) -> List[str]:
        """
        识别需要改进的领域
        
        参数:
            stats: 会话统计数据
            scene_performance: 场景表现列表
            
        返回:
            需要改进的领域列表
        """
        areas = []
        
        if stats.accuracy < 0.5:
            areas.append("可以多加练习，提高互动准确率")
        
        for scene in scene_performance:
            if scene["accuracy"] < 0.4 and scene["total_interactions"] >= 2:
                areas.append(f"建议多加练习{scene['scene_name']}场景")
        
        if stats.total_duration < 180:
            areas.append("可以尝试延长游戏时间，保持更长时间的专注")
        
        return areas

    def export_for_parent(self, format: str = "json",
                          report: Optional[ParentReport] = None) -> str:
        """
        导出为家长端可读的格式
        
        参数:
            format: 导出格式，支持 "json" 或 "txt"
            report: ParentReport对象，不提供则自动生成
            
        返回:
            导出的文件路径
        """
        if report is None:
            report = self.generate_report()
        
        if format == "json":
            return self._export_json(report)
        elif format == "txt":
            return self._export_txt(report)
        else:
            raise ValueError(f"不支持的导出格式: {format}")

    def _export_json(self, report: ParentReport) -> str:
        """
        导出为JSON格式
        
        参数:
            report: ParentReport对象
            
        返回:
            文件路径
        """
        file_name = f"report_{report.session_id}.json"
        file_path = os.path.join(DATA_CONFIG["report_dir"], file_name)
        
        with open(file_path, "w", encoding="utf-8") as f:
            json.dump(report.to_dict(), f, ensure_ascii=False, indent=2)
        
        return file_path

    def _export_txt(self, report: ParentReport) -> str:
        """
        导出为纯文本格式
        
        参数:
            report: ParentReport对象
            
        返回:
            文件路径
        """
        file_name = f"report_{report.session_id}.txt"
        file_path = os.path.join(DATA_CONFIG["report_dir"], file_name)
        
        lines = [
            "=" * 50,
            "宝宝的游戏报告",
            "=" * 50,
            f"报告日期: {report.report_date}",
            f"会话ID: {report.session_id}",
            "",
            "【游戏总结】",
            report.summary_text,
            "",
            "-" * 50,
            "【详细数据】",
            f"总正确率: {int(report.accuracy * 100)}%",
            f"游戏时长: {int(report.total_duration / 60)} 分 {int(report.total_duration % 60)} 秒",
            f"正确互动次数: {report.correct_events}",
            f"需要改进的互动: {report.wrong_events}",
            f"最高连续正确: {report.max_consecutive_correct} 次",
            "",
            "-" * 50,
            "【各场景表现】",
        ]
        
        for scene in report.scene_performance:
            lines.append(
                f"  {scene['scene_name']}: "
                f"{scene['correct_interactions']}/{scene['total_interactions']} "
                f"({int(scene['accuracy'] * 100)}%)"
            )
        
        if report.strengths:
            lines.extend([
                "",
                "-" * 50,
                "【宝宝的优点】",
            ])
            for i, strength in enumerate(report.strengths, 1):
                lines.append(f"  {i}. {strength}")
        
        if report.areas_to_improve:
            lines.extend([
                "",
                "-" * 50,
                "【继续加油的方向】",
            ])
            for i, area in enumerate(report.areas_to_improve, 1):
                lines.append(f"  {i}. {area}")
        
        lines.extend([
            "",
            "=" * 50,
            "宝宝真棒！继续加油哦！🌟",
        ])
        
        with open(file_path, "w", encoding="utf-8") as f:
            f.write("\n".join(lines))
        
        return file_path

    def generate_batch_report(self, session_ids: List[str],
                               format: str = "json") -> Dict[str, str]:
        """
        批量生成多个会话的报告
        
        参数:
            session_ids: 会话ID列表
            format: 导出格式
            
        返回:
            会话ID到文件路径的映射字典
        """
        results = {}
        
        for session_id in session_ids:
            recorder = DataRecorder.load_session(session_id)
            if recorder is not None:
                generator = ReportGenerator(recorder)
                report = generator.generate_report()
                file_path = generator.export_for_parent(format, report)
                results[session_id] = file_path
        
        return results

    def get_recent_reports(self, limit: int = 10) -> List[str]:
        """
        获取最近生成的报告列表
        
        参数:
            limit: 返回数量限制
            
        返回:
            报告文件路径列表
        """
        report_dir = DATA_CONFIG["report_dir"]
        if not os.path.exists(report_dir):
            return []
        
        reports = []
        for file_name in os.listdir(report_dir):
            if file_name.startswith("report_"):
                file_path = os.path.join(report_dir, file_name)
                reports.append(file_path)
        
        reports.sort(key=lambda x: os.path.getmtime(x), reverse=True)
        
        return reports[:limit]

    @staticmethod
    def simulate_send_to_parent(report_data: Dict[str, Any]) -> bool:
        """
        模拟发送报告给家长端
        
        参数:
            report_data: 报告数据字典
            
        返回:
            是否发送成功
        """
        print(f"模拟发送报告给家长端...")
        print(f"报告内容摘要: {report_data.get('summary_text', '无摘要')}")
        print(f"发送成功！")
        return True
