# -*- coding: utf-8 -*-
"""
测试脚本 - 验证各模块功能
Python 3.11 兼容
"""

import json
import time
from config import SCENES
from speech_processor import SpeechProcessor
from data_recorder import DataRecorder, EventCorrectness
from report_generator import ReportGenerator


def test_speech_processor():
    """测试语音处理器"""
    print("=" * 60)
    print("测试第一模块：语音交互与自然语言处理")
    print("=" * 60)
    
    processor = SpeechProcessor()
    
    print(f"\n支持的场景: {processor.get_supported_scenes()}")
    
    test_cases = [
        ("买苹果", "buying_vegetables"),
        ("pingguo", "buying_vegetables"),
        ("涂红色", "coloring"),
        ("hongse", "coloring"),
        ("你好", "buying_vegetables"),
        ("涂蓝色", "coloring"),
        ("要香蕉", "buying_vegetables"),
    ]
    
    print("\n测试文本意图识别:")
    for text, context in test_cases:
        result = processor.parse_intent(text, context)
        print(f"\n  输入: '{text}' (场景: {SCENES.get(context, context)})")
        print(f"    意图: {result.intent}")
        print(f"    实体: {result.entities}")
        print(f"    置信度: {result.confidence}")
    
    print("\n测试语音转文本:")
    audio_tests = ["pingguo.wav", "hongse.wav", "hello.wav"]
    for audio in audio_tests:
        transcribed = processor.transcribe_audio(audio)
        print(f"  {audio} -> '{transcribed}'")
    
    print("\n测试模糊匹配:")
    fuzzy_tests = [
        "pinguo",
        "ping guo",
        "hongse",
        "lvse",
    ]
    for text in fuzzy_tests:
        result = processor.parse_intent(text, "buying_vegetables")
        print(f"  '{text}' -> 意图: {result.intent}, 实体: {result.entities}")


def test_data_recorder():
    """测试数据记录器"""
    print("\n" + "=" * 60)
    print("测试第二模块：数据记录与分析")
    print("=" * 60)
    
    recorder = DataRecorder()
    print(f"\n会话ID: {recorder.session_id}")
    
    print("\n记录一些事件...")
    
    recorder.log_event(
        event_type="scene_start",
        data={"scene": "buying_vegetables"},
        context="buying_vegetables"
    )
    
    recorder.log_event(
        event_type="intent_detected",
        data={"text": "买苹果", "intent": "buy_item", "confidence": 0.9},
        context="buying_vegetables"
    )
    
    recorder.log_event(
        event_type="item_selection",
        data={"selected_item": "苹果", "expected_item": "苹果"},
        context="buying_vegetables"
    )
    
    recorder.log_event(
        event_type="color_selection",
        data={"selected_color": "红色", "expected_color": "红色"},
        context="coloring"
    )
    
    recorder.log_event(
        event_type="color_selection",
        data={"selected_color": "蓝色", "expected_color": "红色"},
        context="coloring"
    )
    
    recorder.log_event(
        event_type="item_selection",
        data={"selected_item": "香蕉", "expected_item": "香蕉"},
        context="buying_vegetables"
    )
    
    recorder.log_event(
        event_type="item_selection",
        data={"selected_item": "橘子", "expected_item": "橘子"},
        context="buying_vegetables"
    )
    
    stats = recorder.get_current_stats()
    print(f"\n当前统计数据:")
    print(f"  正确率: {stats['accuracy'] * 100}%")
    print(f"  总时长: {stats['total_duration']} 秒")
    print(f"  总事件数: {stats['total_events']}")
    print(f"  正确事件: {stats['correct_events']}")
    print(f"  错误事件: {stats['wrong_events']}")
    print(f"  连续正确: {stats['consecutive_correct']}")
    print(f"  最高连续正确: {stats['max_consecutive_correct']}")
    print(f"  是否触发正反馈: {stats['should_trigger_feedback']}")
    print(f"  各场景统计: {stats['scene_stats']}")
    
    correct_events = recorder.get_events_by_correctness(EventCorrectness.CORRECT)
    wrong_events = recorder.get_events_by_correctness(EventCorrectness.WRONG)
    print(f"\n正确事件数: {len(correct_events)}")
    print(f"错误事件数: {len(wrong_events)}")
    
    buying_events = recorder.get_events(context="buying_vegetables")
    print(f"买菜场景事件数: {len(buying_events)}")
    
    final_stats = recorder.end_session()
    print(f"\n会话结束，最终统计:")
    print(f"  总正确率: {final_stats.accuracy * 100}%")
    print(f"  总时长: {final_stats.total_duration:.1f} 秒")
    
    return recorder


def test_report_generator(recorder: DataRecorder):
    """测试报告生成器"""
    print("\n" + "=" * 60)
    print("测试第三模块：家长端数据同步")
    print("=" * 60)
    
    generator = ReportGenerator(recorder)
    
    summary = generator.generate_summary()
    print(f"\n自然语言总结:\n  {summary}")
    
    report = generator.generate_report()
    print(f"\n完整报告:")
    print(f"  报告日期: {report.report_date}")
    print(f"  会话ID: {report.session_id}")
    print(f"  正确率: {report.accuracy * 100}%")
    print(f"  总时长: {report.total_duration:.1f} 秒")
    print(f"  正确互动: {report.correct_events} 次")
    print(f"  错误互动: {report.wrong_events} 次")
    print(f"  最高连续正确: {report.max_consecutive_correct} 次")
    
    print(f"\n各场景表现:")
    for scene in report.scene_performance:
        print(f"  {scene['scene_name']}: "
              f"{scene['correct_interactions']}/{scene['total_interactions']} "
              f"({scene['accuracy'] * 100}%)")
    
    print(f"\n宝宝的优点:")
    for i, strength in enumerate(report.strengths, 1):
        print(f"  {i}. {strength}")
    
    if report.areas_to_improve:
        print(f"\n需要改进的方向:")
        for i, area in enumerate(report.areas_to_improve, 1):
            print(f"  {i}. {area}")
    
    json_path = generator.export_for_parent("json", report)
    txt_path = generator.export_for_parent("txt", report)
    
    print(f"\n报告已导出:")
    print(f"  JSON格式: {json_path}")
    print(f"  TXT格式: {txt_path}")
    
    recent_reports = generator.get_recent_reports(5)
    print(f"\n最近的报告列表 ({len(recent_reports)}个):")
    for report_path in recent_reports:
        print(f"  {report_path}")


def main():
    """主测试函数"""
    print("\n" + "#" * 60)
    print("# Unity + Python 具身多模态交互项目 - Python端测试")
    print("#" * 60)
    
    test_speech_processor()
    recorder = test_data_recorder()
    test_report_generator(recorder)
    
    print("\n" + "=" * 60)
    print("所有测试完成！")
    print("=" * 60)


if __name__ == "__main__":
    main()
