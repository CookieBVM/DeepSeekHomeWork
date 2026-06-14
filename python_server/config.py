# -*- coding: utf-8 -*-
"""
配置文件模块
定义项目全局配置常量
Python 3.11 兼容
"""

# 语音识别相关配置
SPEECH_CONFIG = {
    # 模糊匹配阈值（Levenshtein距离），值越小匹配越严格
    "fuzzy_match_threshold": 3,
    # 置信度阈值
    "confidence_threshold": 0.6,
}

# 场景定义
SCENES = {
    "buying_vegetables": "买菜场景",
    "coloring": "涂色场景",
}

# 买菜场景意图库
BUYING_VEGETABLES_INTENTS = {
    "buy_item": {
        "keywords": ["买", "要", "拿", "来点", "给我"],
        "entities": {
            "items": {
                "苹果": ["苹果", "pingguo", "pinguo", "ping guo", "apple"],
                "香蕉": ["香蕉", "xiangjiao", "xiang jiao", "banana"],
                "橘子": ["橘子", "橘子", "juzi", "ju zi", "orange"],
                "白菜": ["白菜", "baicai", "bai cai", "cabbage"],
                "胡萝卜": ["胡萝卜", "huluobo", "hu luo bo", "carrot"],
                "西红柿": ["西红柿", "xihongshi", "xi hong shi", "番茄", "tomato"],
                "土豆": ["土豆", "tudou", "tu dou", "potato"],
                "黄瓜": ["黄瓜", "huanggua", "huang gua", "cucumber"],
                "洋葱": ["洋葱", "yangcong", "yang cong", "onion"],
                "茄子": ["茄子", "qiezi", "qie zi", "eggplant"],
            }
        },
    },
    "ask_price": {
        "keywords": ["多少钱", "价格", "怎么卖", "几元", "几块"],
        "entities": {},
    },
    "greet": {
        "keywords": ["你好", "您好", "嗨", "哈喽", "hello", "hi"],
        "entities": {},
    },
    "thanks": {
        "keywords": ["谢谢", "感谢", "thank", "thanks"],
        "entities": {},
    },
    "bye": {
        "keywords": ["再见", "拜拜", "bye", "走了"],
        "entities": {},
    },
}

# 涂色场景意图库
COLORING_INTENTS = {
    "color_object": {
        "keywords": ["涂", "画", "颜色", "上色"],
        "entities": {
            "colors": {
                "红色": ["红色", "红", "hongse", "hong se", "red"],
                "蓝色": ["蓝色", "蓝", "lanse", "lan se", "blue"],
                "绿色": ["绿色", "绿", "lvse", "lv se", "green"],
                "黄色": ["黄色", "黄", "huangse", "huang se", "yellow"],
                "紫色": ["紫色", "紫", "zise", "zi se", "purple"],
                "橙色": ["橙色", "橙", "chengse", "cheng se", "orange"],
                "粉色": ["粉色", "粉", "fense", "fen se", "pink"],
                "黑色": ["黑色", "黑", "heise", "hei se", "black"],
                "白色": ["白色", "白", "baise", "bai se", "white"],
                "棕色": ["棕色", "棕", "zongse", "zong se", "brown"],
            }
        },
    },
    "select_object": {
        "keywords": ["选", "这个", "那个", "这个"],
        "entities": {},
    },
    "finish": {
        "keywords": ["完成", "好了", "结束", "done", "finish"],
        "entities": {},
    },
}

# 数据记录配置
DATA_CONFIG = {
    # 离线缓存目录
    "cache_dir": "data/cache",
    # 日志目录
    "log_dir": "data/logs",
    # 报告输出目录
    "report_dir": "data/reports",
    # 缓存文件格式：json 或 pickle
    "cache_format": "json",
    # 连续正确次数阈值（触发正反馈）
    "streak_threshold": 3,
}

# 事件类型定义
EVENT_TYPES = {
    # 语音相关事件
    "speech_start": "开始说话",
    "speech_end": "结束说话",
    "speech_recognized": "语音识别完成",
    
    # 交互事件
    "intent_detected": "意图检测",
    "correct_action": "正确操作",
    "wrong_action": "错误操作",
    
    # 场景事件
    "scene_start": "场景开始",
    "scene_end": "场景结束",
    
    # 正反馈事件
    "positive_feedback": "正反馈触发",
}

# Unity 通信配置
COMMUNICATION_CONFIG = {
    # 通信方式：stdin_stdout 或 file_watch
    "mode": "stdin_stdout",
    # 文件监听间隔（秒）
    "watch_interval": 0.5,
    # 输入文件路径（file_watch模式）
    "input_file": "data/communication/unity_to_python.json",
    # 输出文件路径（file_watch模式）
    "output_file": "data/communication/python_to_unity.json",
}
