# -*- coding: utf-8 -*-
"""
实时日志监控脚本
在独立终端窗口中实时显示Python统计数据
用法: python tail_log.py <日志文件路径>
"""

import sys
import os
import time
import argparse
from datetime import datetime


class LogTailer:
    """日志文件尾部监控器"""

    def __init__(self, log_file, follow=True):
        self.log_file = log_file
        self.follow = follow

    def _print_banner(self):
        print("")
        print("=" * 70)
        print("                    【Python实时统计监视器】")
        print("=" * 70)
        print("  监控文件: " + self.log_file)
        print("  启动时间: " + datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
        print("=" * 70)
        print("  提示:")
        print("    * 此窗口将实时显示Unity游戏统计数据")
        print("    * Unity中开始游戏后，数据会实时显示")
        print("    * 按 Ctrl+C 退出")
        print("=" * 70)
        print("")

    def run(self):
        self._print_banner()

        log_dir = os.path.dirname(self.log_file)
        if log_dir and not os.path.exists(log_dir):
            os.makedirs(log_dir)

        try:
            with open(self.log_file, 'r', encoding='utf-8') as f:
                f.seek(0, 2)

                print("")
                print("[就绪] 已连接到Unity，等待数据...")
                print("")

                while self.follow:
                    line = f.readline()
                    if not line:
                        time.sleep(0.1)
                        continue

                    line = line.rstrip()
                    if line:
                        print(line)

        except KeyboardInterrupt:
            print("")
            print("[退出] 监控已停止")
            print("=" * 70)
        except Exception as e:
            print("")
            print("[错误] " + str(e))
            print("=" * 70)


def main():
    parser = argparse.ArgumentParser(description='实时日志监控')
    parser.add_argument('log_file', nargs='?',
                       default=None,
                       help='日志文件路径')
    parser.add_argument('--no-follow', dest='follow',
                       action='store_false',
                       help='只显示现有内容后退出')

    args = parser.parse_args()

    if args.log_file:
        log_file = args.log_file
    else:
        log_dir = os.path.join(
            os.path.dirname(os.path.abspath(__file__)),
            'data',
            'logs'
        )
        log_file = os.path.join(log_dir, 'latest_stats_实时监控.log')

    log_dir = os.path.dirname(log_file)
    if log_dir and not os.path.exists(log_dir):
        os.makedirs(log_dir)

    tailer = LogTailer(log_file, follow=args.follow)
    tailer.run()


if __name__ == '__main__':
    main()
