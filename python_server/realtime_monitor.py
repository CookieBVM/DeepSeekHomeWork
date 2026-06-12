# -*- coding: utf-8 -*-
"""
实时统计监视器
- 同时输出到控制台和日志文件
- 可以用文本编辑器/VS Code实时查看
- Python 3.11 兼容
"""

import os
import time
from datetime import datetime

from utf8_output import safe_print


class RealtimeStatsMonitor:
    """实时统计监视器类"""

    def __init__(self, log_file=None):
        self.session_started = False
        self.session_id = None
        self.current_module = None
        self.current_scenario = None
        self.current_difficulty = None
        self.start_time = None
        self.end_time = None
        
        self.total_events = 0
        self.total_interactions = 0
        self.correct_events = 0
        self.wrong_events = 0
        self.unknown_events = 0
        self.consecutive_correct = 0
        self.max_consecutive_correct = 0
        
        self.input_mode_stats = {
            "语音识别": {"total": 0, "correct": 0},
            "文本输入": {"total": 0, "correct": 0},
            "选项选择": {"total": 0, "correct": 0},
        }
        self.participant_stats = {
            "孩子": {"total": 0, "correct": 0},
            "家长": {"total": 0, "correct": 0},
            "系统": {"total": 0, "correct": 0},
        }
        
        self.elapsed_seconds_list = []
        self.completed_tasks = 0
        self.reward_count = 0

        if log_file is None:
            self.log_file = os.path.join(
                os.path.dirname(os.path.abspath(__file__)),
                "data",
                "logs",
                "latest_stats_实时监控.log"
            )
        else:
            self.log_file = log_file

        self._ensure_log_dir()
        self._write_log_header()

        safe_print("")
        safe_print("=" * 70)
        safe_print("                    实时统计监视器已启动")
        safe_print("=" * 70)
        safe_print("  日志文件: " + self.log_file)
        safe_print("=" * 70)
        safe_print("  提示:")
        safe_print("    1. 可以用 VS Code 或其他文本编辑器打开此日志文件")
        safe_print("    2. 启用 '自动刷新' 或手动刷新以查看实时更新")
        safe_print("    3. 当 Unity 中开始游戏时，统计数据将实时写入这里")
        safe_print("=" * 70)
        safe_print("")
        safe_print("[就绪] 等待 Unity 游戏开始...")
        safe_print("")

    def _ensure_log_dir(self):
        log_dir = os.path.dirname(self.log_file)
        if log_dir and not os.path.exists(log_dir):
            os.makedirs(log_dir, exist_ok=True)

    def _write_log_header(self):
        with open(self.log_file, "a", encoding="utf-8") as f:
            f.write("\n" + "=" * 70 + "\n")
            f.write("实时统计监视器启动 - " + datetime.now().strftime('%Y-%m-%d %H:%M:%S') + "\n")
            f.write("=" * 70 + "\n\n")

    def _write_to_file(self, message):
        try:
            self._ensure_log_dir()
            with open(self.log_file, "a", encoding="utf-8") as f:
                f.write(message + "\n")
        except Exception as e:
            try:
                safe_print("写入日志文件失败: " + str(e))
            except:
                pass

    def print_and_log(self, message):
        safe_print(message)
        self._write_to_file(message)

    def _get_accuracy_label(self, accuracy):
        if accuracy >= 0.85:
            return "优秀"
        elif accuracy >= 0.7:
            return "良好"
        elif accuracy >= 0.5:
            return "一般"
        elif accuracy >= 0.3:
            return "需努力"
        else:
            return "待提高"

    def _format_duration(self, seconds):
        if seconds is None or seconds <= 0:
            return "0秒"
        minutes = int(seconds // 60)
        secs = int(seconds % 60)
        if minutes > 0:
            return f"{minutes}分{secs}秒"
        return f"{secs}秒"

    def _calculate_avg_time(self):
        if len(self.elapsed_seconds_list) == 0:
            return 0.0
        return sum(self.elapsed_seconds_list) / len(self.elapsed_seconds_list)

    def _calculate_interactions_per_minute(self):
        if not self.start_time:
            return 0.0
        elapsed = time.time() - self.start_time
        if elapsed < 1:
            return 0.0
        return self.total_interactions / (elapsed / 60)

    def handle_session_started(self, data):
        self.session_started = True
        self.session_id = data.get("session_id", "unknown")
        self.current_module = data.get("module", "未知模块")
        self.current_scenario = data.get("scenario", "未知场景")
        self.current_difficulty = data.get("difficulty", "未知难度")
        self.start_time = time.time()
        self.end_time = None
        
        self.total_events = 0
        self.total_interactions = 0
        self.correct_events = 0
        self.wrong_events = 0
        self.unknown_events = 0
        self.consecutive_correct = 0
        self.max_consecutive_correct = 0
        
        self.input_mode_stats = {
            "语音识别": {"total": 0, "correct": 0},
            "文本输入": {"total": 0, "correct": 0},
            "选项选择": {"total": 0, "correct": 0},
        }
        self.participant_stats = {
            "孩子": {"total": 0, "correct": 0},
            "家长": {"total": 0, "correct": 0},
            "系统": {"total": 0, "correct": 0},
        }
        
        self.elapsed_seconds_list = []
        self.completed_tasks = 0
        self.reward_count = 0

        self.print_and_log("")
        self.print_and_log("=" * 70)
        self.print_and_log("                    新会话开始")
        self.print_and_log("=" * 70)
        self.print_and_log("  会话ID: " + self.session_id)
        self.print_and_log("  开始时间: " + datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
        self.print_and_log("  模块: " + self.current_module)
        self.print_and_log("  场景: " + self.current_scenario)
        self.print_and_log("  难度: " + self.current_difficulty)
        self.print_and_log("=" * 70)
        self.print_and_log("  当前统计:")
        self.print_and_log("    总交互数: 0")
        self.print_and_log("    正确次数: 0")
        self.print_and_log("    错误次数: 0")
        self.print_and_log("    正确率: 0%")
        self.print_and_log("    连续正确: 0")
        self.print_and_log("    最高连续正确: 0")
        self.print_and_log("=" * 70)
        self.print_and_log("")
        self.print_and_log("[提示] 开始游戏交互，统计数据将实时更新...")
        self.print_and_log("")

    def handle_interaction(self, data, stats):
        self.total_events += 1
        self.total_interactions += 1

        event_type = data.get("event_type", "unknown")

        if event_type == "session_started":
            self.handle_session_started(data)
            return

        input_content = data.get("input", "")
        response = data.get("response", "")
        is_correct = data.get("is_correct")
        participant = data.get("participant", "")
        input_mode = data.get("input_mode", "")
        elapsed_seconds = data.get("elapsed_seconds")

        if is_correct is not None:
            if is_correct:
                self.correct_events += 1
                self.consecutive_correct += 1
                if self.consecutive_correct > self.max_consecutive_correct:
                    self.max_consecutive_correct = self.consecutive_correct
            else:
                self.wrong_events += 1
                self.consecutive_correct = 0
        else:
            self.unknown_events += 1

        if input_mode and input_mode in self.input_mode_stats:
            self.input_mode_stats[input_mode]["total"] += 1
            if is_correct:
                self.input_mode_stats[input_mode]["correct"] += 1

        if participant and participant in self.participant_stats:
            self.participant_stats[participant]["total"] += 1
            if is_correct:
                self.participant_stats[participant]["correct"] += 1

        if elapsed_seconds is not None and elapsed_seconds > 0:
            self.elapsed_seconds_list.append(elapsed_seconds)

        self.print_and_log("-" * 70)
        self.print_and_log("新交互记录 [" + datetime.now().strftime('%H:%M:%S') + "]")
        self.print_and_log("-" * 70)

        if participant:
            self.print_and_log("  参与者: " + participant)
        if input_mode:
            self.print_and_log("  输入方式: " + input_mode)
        if input_content:
            self.print_and_log('  输入内容: "' + input_content + '"')
        if response:
            self.print_and_log('  数字人回复: "' + response + '"')

        if is_correct is not None:
            if is_correct:
                self.print_and_log("  结果: 正确")
            else:
                self.print_and_log("  结果: 错误")
        else:
            self.print_and_log("  结果: 未判定")

        self.print_and_log("  当前连续正确: " + str(self.consecutive_correct))

        if self.consecutive_correct >= 5:
            self.print_and_log("  [优秀] 太棒了! 连续 " + str(self.consecutive_correct) + " 次正确!")
        elif self.consecutive_correct >= 3:
            self.print_and_log("  [很好] 连胜中! 已连续 " + str(self.consecutive_correct) + " 次正确")

        self.print_and_log("-" * 70)

        if stats:
            self._update_stats_from_dict(stats)
        self._print_current_stats()

    def _update_stats_from_dict(self, stats):
        if "correct_events" in stats:
            self.correct_events = stats["correct_events"]
        if "wrong_events" in stats:
            self.wrong_events = stats["wrong_events"]
        if "consecutive_correct" in stats:
            self.consecutive_correct = stats["consecutive_correct"]
        if "max_consecutive_correct" in stats:
            self.max_consecutive_correct = stats["max_consecutive_correct"]
        if "completed_tasks" in stats:
            self.completed_tasks = stats["completed_tasks"]

    def _print_current_stats(self):
        total = self.total_interactions
        correct = self.correct_events
        wrong = self.wrong_events

        accuracy = 0.0
        if correct + wrong > 0:
            accuracy = correct / (correct + wrong)

        accuracy_label = self._get_accuracy_label(accuracy)

        duration = 0.0
        if self.start_time:
            duration = time.time() - self.start_time

        self.print_and_log("")
        self.print_and_log("=" * 70)
        self.print_and_log("                    实时统计数据")
        self.print_and_log("=" * 70)
        self.print_and_log("  总交互数: " + str(total))
        self.print_and_log("  正确次数: " + str(correct))
        self.print_and_log("  错误次数: " + str(wrong))
        self.print_and_log("  正确率: " + str(int(accuracy * 100)) + "% (" + accuracy_label + ")")
        self.print_and_log("  当前连续正确: " + str(self.consecutive_correct))
        self.print_and_log("  最高连续正确: " + str(self.max_consecutive_correct))
        self.print_and_log("  当前时长: " + str(duration)[:5] + "秒")
        
        if self.completed_tasks > 0:
            self.print_and_log("  已完成任务: " + str(self.completed_tasks))
        
        self.print_and_log("=" * 70)
        self.print_and_log("")

    def handle_stats_updated(self, stats):
        self.total_events = stats.get("total_interactions", self.total_events)
        self.total_interactions = stats.get("total_interactions", self.total_interactions)
        self.correct_events = stats.get("correct", self.correct_events)
        self.wrong_events = stats.get("wrong", self.wrong_events)
        self.completed_tasks = stats.get("completed_tasks", self.completed_tasks)
        
        max_cc = stats.get("max_consecutive_correct", 0)
        if max_cc > self.max_consecutive_correct:
            self.max_consecutive_correct = max_cc

    def handle_session_ended(self, data):
        self.end_time = time.time()
        
        final_stats = data.get("final_stats", {})
        if final_stats:
            self.total_events = final_stats.get("total_events", self.total_events)
            self.correct_events = final_stats.get("correct_events", self.correct_events)
            self.wrong_events = final_stats.get("wrong_events", self.wrong_events)
        
        self._print_session_ended()
        
        self.session_started = False

    def _print_session_ended(self):
        self.print_and_log("")
        self.print_and_log("=" * 70)
        self.print_and_log("                    会话结束!")
        self.print_and_log("=" * 70)

        total = self.total_interactions
        correct = self.correct_events
        wrong = self.wrong_events
        max_streak = self.max_consecutive_correct

        accuracy = 0.0
        if correct + wrong > 0:
            accuracy = correct / (correct + wrong)

        accuracy_label = self._get_accuracy_label(accuracy)

        duration = 0.0
        if self.start_time:
            if self.end_time:
                duration = self.end_time - self.start_time
            else:
                duration = time.time() - self.start_time

        total_minutes = int(duration // 60)
        total_seconds = int(duration % 60)
        
        avg_time = self._calculate_avg_time()
        ipm = self._calculate_interactions_per_minute()

        self.print_and_log("")
        self.print_and_log("-" * 70)
        self.print_and_log("  【最终报告摘要】")
        self.print_and_log("-" * 70)
        
        self.print_and_log("  游戏模块: " + str(self.current_module))
        self.print_and_log("  场景: " + str(self.current_scenario))
        self.print_and_log("  难度: " + str(self.current_difficulty))
        self.print_and_log("  总时长: " + str(total_minutes) + " 分 " + str(total_seconds) + " 秒")
        
        self.print_and_log("")
        self.print_and_log("  总正确率: " + str(int(accuracy * 100)) + "% (" + accuracy_label + ")")
        self.print_and_log("  正确互动: " + str(correct))
        self.print_and_log("  需要改进: " + str(wrong))
        self.print_and_log("  最高连续正确: " + str(max_streak) + " 次")
        
        if self.completed_tasks > 0:
            self.print_and_log("  完成任务数: " + str(self.completed_tasks))
        
        self.print_and_log("")
        self.print_and_log("  平均每次耗时: " + f"{avg_time:.2f}" + "秒")
        self.print_and_log("  每分钟交互数: " + f"{ipm:.2f}")
        
        self._print_input_mode_summary()
        self._print_participant_summary()
        
        self.print_and_log("-" * 70)
        self.print_and_log("")
        
        if accuracy >= 0.7:
            self.print_and_log("                    宝宝真棒! 表现非常出色!")
        elif accuracy >= 0.5:
            self.print_and_log("                    宝宝加油! 继续努力会更好!")
        else:
            self.print_and_log("                    多多练习，下次会更好!")
        
        self.print_and_log("=" * 70)
        self.print_and_log("")
        self.print_and_log("[会话已结束] 统计数据已保存")
        self.print_and_log("[提示] 等待下一次游戏开始...")
        self.print_and_log("")

    def _print_input_mode_summary(self):
        has_data = any(stats["total"] > 0 for stats in self.input_mode_stats.values())
        if not has_data:
            return
        
        self.print_and_log("")
        self.print_and_log("  【输入方式统计】")
        self.print_and_log("  " + "-" * 40)
        self.print_and_log("  输入方式          | 总次数 | 正确 | 正确率")
        self.print_and_log("  " + "-" * 40)
        
        for mode, stats in self.input_mode_stats.items():
            total = stats["total"]
            if total == 0:
                continue
            correct = stats["correct"]
            acc = correct / total if total > 0 else 0.0
            self.print_and_log(f"  {mode:<16} | {total:>6} | {correct:>4} | {acc * 100:>6.1f}%")

    def _print_participant_summary(self):
        has_data = any(stats["total"] > 0 for stats in self.participant_stats.values())
        if not has_data:
            return
        
        self.print_and_log("")
        self.print_and_log("  【参与者统计】")
        self.print_and_log("  " + "-" * 38)
        self.print_and_log("  参与者  | 总交互 | 正确 | 正确率")
        self.print_and_log("  " + "-" * 38)
        
        for participant, stats in self.participant_stats.items():
            total = stats["total"]
            if total == 0:
                continue
            correct = stats["correct"]
            acc = correct / total if total > 0 else 0.0
            self.print_and_log(f"  {participant:<6} | {total:>6} | {correct:>4} | {acc * 100:>6.1f}%")

    def handle_event(self, event_type, data, stats=None):
        if event_type == "session_started":
            self.handle_session_started(data)
        elif event_type == "stats_updated":
            self.handle_stats_updated(data)
        elif event_type == "session_ended":
            self.handle_session_ended(data)
        elif event_type == "interaction_recorded":
            self.handle_interaction(data, stats or {})
        elif event_type == "task_completed":
            self.completed_tasks += 1
            self.print_and_log("")
            self.print_and_log("=" * 70)
            self.print_and_log("                    任务完成")
            self.print_and_log("=" * 70)
            self.print_and_log("  已完成任务数: " + str(self.completed_tasks))
            self.print_and_log("=" * 70)
            self.print_and_log("")
        elif event_type == "reward_requested":
            self.reward_count += 1
            self.print_and_log("")
            self.print_and_log("=" * 70)
            self.print_and_log("                    正反馈触发")
            self.print_and_log("=" * 70)
            self.print_and_log("  累计正反馈次数: " + str(self.reward_count))
            self.print_and_log("=" * 70)
            self.print_and_log("")
        else:
            self.handle_interaction(data, stats or {})

    def handle_raw_event_data(self, event_data):
        event_type = event_data.get("event_type", "unknown")
        data = event_data.get("data", {})
        stats = event_data.get("stats", None)

        self.handle_event(event_type, data, stats)


monitor = RealtimeStatsMonitor()
