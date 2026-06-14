# -*- coding: utf-8 -*-
"""
UTF-8 输出处理模块
确保在 Windows 环境下所有输出都能正确处理中文
"""

import sys
import os
import io
import json
from typing import Any


def setup_utf8_encoding() -> None:
    """设置标准输入输出为 UTF-8 编码"""
    if sys.platform == "win32":
        os.environ["PYTHONIOENCODING"] = "utf-8"
        os.environ["PYTHONUTF8"] = "1"
        
        try:
            import codecs
            if hasattr(sys.stdout, "buffer"):
                sys.stdout = codecs.getwriter("utf-8")(sys.stdout.buffer)
            if hasattr(sys.stderr, "buffer"):
                sys.stderr = codecs.getwriter("utf-8")(sys.stderr.buffer)
            if hasattr(sys.stdin, "buffer"):
                sys.stdin = codecs.getreader("utf-8")(sys.stdin.buffer)
        except Exception:
            try:
                if hasattr(sys.stdout, "buffer"):
                    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
                if hasattr(sys.stderr, "buffer"):
                    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", line_buffering=True)
                if hasattr(sys.stdin, "buffer"):
                    sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8")
            except Exception:
                pass


def safe_print(obj: Any, flush: bool = True) -> None:
    """
    安全的 UTF-8 输出函数
    
    参数:
        obj: 要输出的对象，如果是 dict 会自动序列化为 JSON
        flush: 是否立即刷新缓冲区
    """
    try:
        if isinstance(obj, dict):
            s = json.dumps(obj, ensure_ascii=False)
        else:
            s = str(obj)
        
        if sys.platform == "win32":
            if hasattr(sys.stdout, "buffer"):
                sys.stdout.buffer.write((s + "\n").encode("utf-8"))
                if flush:
                    sys.stdout.buffer.flush()
                return
        
        print(s, flush=flush)
    except Exception:
        try:
            print(str(obj), flush=flush)
        except Exception:
            pass


def safe_error(obj: Any, flush: bool = True) -> None:
    """
    安全的 UTF-8 错误输出函数
    
    参数:
        obj: 要输出的对象
        flush: 是否立即刷新缓冲区
    """
    try:
        s = str(obj)
        
        if sys.platform == "win32":
            if hasattr(sys.stderr, "buffer"):
                sys.stderr.buffer.write((s + "\n").encode("utf-8"))
                if flush:
                    sys.stderr.buffer.flush()
                return
        
        print(s, file=sys.stderr, flush=flush)
    except Exception:
        try:
            print(str(obj), file=sys.stderr, flush=flush)
        except Exception:
            pass


def to_utf8_json(obj: dict, indent: int = None) -> str:
    """
    将字典转换为 UTF-8 编码的 JSON 字符串（不含 BOM）
    
    参数:
        obj: 要转换的字典
        indent: 缩进级别，None 表示不格式化
    
    返回:
        UTF-8 编码的 JSON 字符串
    """
    return json.dumps(obj, ensure_ascii=False, indent=indent)


def write_utf8_file(file_path: str, content: Any, indent: int = 2) -> None:
    """
    将内容以 UTF-8 编码写入文件
    
    参数:
        file_path: 文件路径
        content: 要写入的内容，如果是 dict 会自动序列化为 JSON
        indent: JSON 缩进级别
    """
    if isinstance(content, dict):
        text = to_utf8_json(content, indent=indent)
    else:
        text = str(content)
    
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(text)


def read_utf8_file(file_path: str) -> str:
    """
    以 UTF-8 编码读取文件
    
    参数:
        file_path: 文件路径
    
    返回:
        文件内容字符串
    """
    with open(file_path, "r", encoding="utf-8") as f:
        return f.read()
