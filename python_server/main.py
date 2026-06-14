# -*- coding: utf-8 -*-
"""
Python端主入口与Unity通信接口
支持通过标准输入输出(sys.stdin/sys.stdout)和文件监听两种方式与Unity通信
Python 3.11 兼容
"""

import json
import sys
import time
import os
from typing import Dict, Any, Optional
from datetime import datetime

from config import COMMUNICATION_CONFIG, DATA_CONFIG
from speech_processor import SpeechProcessor
from data_recorder import DataRecorder
from report_generator import ReportGenerator
from utf8_output import setup_utf8_encoding, safe_print
from realtime_monitor import monitor


class UnityBridge:
    """Unity通信桥接类，处理与Unity的数据交互"""
    
    def __init__(self):
        """初始化通信桥接"""
        self.speech_processor = SpeechProcessor()
        self.data_recorder = DataRecorder()
        self.report_generator = ReportGenerator(self.data_recorder)
        
        self.mode = COMMUNICATION_CONFIG.get("mode", "stdin_stdout")
        self.watch_interval = COMMUNICATION_CONFIG.get("watch_interval", 0.5)
        self.input_file = COMMUNICATION_CONFIG.get("input_file")
        self.output_file = COMMUNICATION_CONFIG.get("output_file")
        
        self._running = False
        self._last_input_file_mtime = 0

    def start(self):
        """启动服务"""
        safe_print(f"[{datetime.now()}] Python服务启动，通信模式: {self.mode}")
        
        if self.mode == "stdin_stdout":
            self._run_stdin_stdout_mode()
        elif self.mode == "file_watch":
            self._run_file_watch_mode()
        else:
            safe_print(f"不支持的通信模式: {self.mode}")

    def _run_stdin_stdout_mode(self):
        """运行标准输入输出模式"""
        self._running = True
        
        while self._running:
            try:
                line = sys.stdin.readline()
                if not line:
                    continue
                
                response = self._process_message(line.strip())
                if response:
                    safe_print(response)
            except KeyboardInterrupt:
                safe_print(f"[{datetime.now()}] 收到中断信号，服务停止")
                self._running = False
            except Exception as e:
                error_response = self._create_error_response(str(e))
                safe_print(error_response)

    def _run_file_watch_mode(self):
        """运行文件监听模式"""
        self._running = True
        
        self._ensure_communication_dir()
        
        safe_print(f"[{datetime.now()}] 开始监听输入文件: {self.input_file}")
        
        while self._running:
            try:
                if os.path.exists(self.input_file):
                    current_mtime = os.path.getmtime(self.input_file)
                    
                    if current_mtime != self._last_input_file_mtime:
                        self._last_input_file_mtime = current_mtime
                        
                        with open(self.input_file, "r", encoding="utf-8") as f:
                            message = f.read()
                        
                        response = self._process_message(message)
                        if response:
                            self._write_output(response)
                        
                        try:
                            os.remove(self.input_file)
                        except OSError:
                            pass
                
                time.sleep(self.watch_interval)
            except KeyboardInterrupt:
                safe_print(f"[{datetime.now()}] 收到中断信号，服务停止")
                self._running = False
            except Exception as e:
                error_response = self._create_error_response(str(e))
                self._write_output(error_response)
                time.sleep(self.watch_interval)

    def _ensure_communication_dir(self):
        """确保通信目录存在"""
        comm_dir = os.path.dirname(self.input_file)
        if comm_dir and not os.path.exists(comm_dir):
            os.makedirs(comm_dir, exist_ok=True)

    def _write_output(self, response: Dict[str, Any]):
        """写入输出文件"""
        with open(self.output_file, "w", encoding="utf-8") as f:
            json.dump(response, f, ensure_ascii=False, indent=2)

    def _process_message(self, message: str) -> Optional[Dict[str, Any]]:
        """
        处理来自Unity的消息
        
        参数:
            message: JSON格式的消息字符串
            
        返回:
            响应字典或None
        """
        if not message:
            return None
        
        try:
            data = json.loads(message)
        except json.JSONDecodeError as e:
            return self._create_error_response(f"JSON解析错误: {str(e)}")
        
        action = data.get("action")
        
        handlers = {
            "transcribe_audio": self._handle_transcribe_audio,
            "parse_intent": self._handle_parse_intent,
            "log_event": self._handle_log_event,
            "get_stats": self._handle_get_stats,
            "generate_report": self._handle_generate_report,
            "export_report": self._handle_export_report,
            "end_session": self._handle_end_session,
            "ping": self._handle_ping,
            "list_sessions": self._handle_list_sessions,
            "load_session": self._handle_load_session,
        }
        
        handler = handlers.get(action)
        if handler:
            return handler(data)
        else:
            return self._create_error_response(f"未知的操作: {action}")

    def _handle_transcribe_audio(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理语音转文本请求"""
        audio_path = data.get("audio_path", "")
        context = data.get("context", "buying_vegetables")
        
        transcribed_text = self.speech_processor.transcribe_audio(audio_path)
        
        return {
            "success": True,
            "action": "transcribe_audio",
            "data": {
                "text": transcribed_text,
                "audio_path": audio_path
            },
            "timestamp": time.time()
        }

    def _handle_parse_intent(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理意图分析请求"""
        text = data.get("text", "")
        context = data.get("context", "buying_vegetables")
        
        intent_result = self.speech_processor.parse_intent(text, context)
        
        self.data_recorder.log_event(
            event_type="intent_detected",
            data={
                "text": text,
                "intent": intent_result.intent,
                "entities": intent_result.entities,
                "confidence": intent_result.confidence
            },
            context=context
        )
        
        return {
            "success": True,
            "action": "parse_intent",
            "data": {
                "intent": intent_result.intent,
                "entities": intent_result.entities,
                "confidence": intent_result.confidence,
                "raw_text": intent_result.raw_text
            },
            "timestamp": time.time()
        }

    def _handle_log_event(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理事件记录请求"""
        event_type = data.get("event_type", "unknown")
        event_data = data.get("data", {})
        context = data.get("context", "unknown")
        
        event = self.data_recorder.log_event(event_type, event_data, context)
        
        stats = self.data_recorder.get_current_stats()
        
        monitor.handle_event(event_type, event_data, stats)
        
        return {
            "success": True,
            "action": "log_event",
            "data": {
                "event_id": event.event_id,
                "correctness": event.correctness.value,
                "stats": stats
            },
            "timestamp": time.time()
        }

    def _handle_get_stats(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理获取统计数据请求"""
        stats = self.data_recorder.get_current_stats()
        
        return {
            "success": True,
            "action": "get_stats",
            "data": stats,
            "timestamp": time.time()
        }

    def _handle_generate_report(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理生成报告请求"""
        report = self.report_generator.generate_report()
        
        return {
            "success": True,
            "action": "generate_report",
            "data": report.to_dict(),
            "timestamp": time.time()
        }

    def _handle_export_report(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理导出报告请求"""
        export_format = data.get("format", "json")
        
        report = self.report_generator.generate_report()
        file_path = self.report_generator.export_for_parent(export_format, report)
        
        ReportGenerator.simulate_send_to_parent(report.to_dict())
        
        return {
            "success": True,
            "action": "export_report",
            "data": {
                "file_path": file_path,
                "format": export_format,
                "report": report.to_dict()
            },
            "timestamp": time.time()
        }

    def _handle_end_session(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理结束会话请求"""
        final_stats = self.data_recorder.end_session()
        
        report = self.report_generator.generate_report()
        
        monitor.handle_session_ended({
            "session_id": self.data_recorder.session_id,
            "final_stats": final_stats.to_dict()
        })
        
        return {
            "success": True,
            "action": "end_session",
            "data": {
                "session_id": self.data_recorder.session_id,
                "final_stats": final_stats.to_dict(),
                "report": report.to_dict()
            },
            "timestamp": time.time()
        }

    def _handle_ping(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理心跳请求"""
        return {
            "success": True,
            "action": "ping",
            "data": {
                "status": "alive",
                "session_id": self.data_recorder.session_id
            },
            "timestamp": time.time()
        }

    def _handle_list_sessions(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理列出会话请求"""
        sessions = DataRecorder.get_available_sessions()
        
        return {
            "success": True,
            "action": "list_sessions",
            "data": {
                "sessions": sessions
            },
            "timestamp": time.time()
        }

    def _handle_load_session(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """处理加载会话请求"""
        session_id = data.get("session_id")
        
        if not session_id:
            return self._create_error_response("缺少session_id参数")
        
        loaded_recorder = DataRecorder.load_session(session_id)
        
        if loaded_recorder:
            self.data_recorder = loaded_recorder
            self.report_generator = ReportGenerator(self.data_recorder)
            
            return {
                "success": True,
                "action": "load_session",
                "data": {
                    "session_id": session_id,
                    "stats": loaded_recorder.get_current_stats()
                },
                "timestamp": time.time()
            }
        else:
            return self._create_error_response(f"找不到会话: {session_id}")

    def _create_error_response(self, message: str) -> Dict[str, Any]:
        """
        创建错误响应
        
        参数:
            message: 错误消息
            
        返回:
            错误响应字典
        """
        return {
            "success": False,
            "error": message,
            "timestamp": time.time()
        }


def main():
    """主函数"""
    setup_utf8_encoding()
    bridge = UnityBridge()
    bridge.start()


if __name__ == "__main__":
    main()
