# -*- coding: utf-8 -*-
"""
语音交互与自然语言处理模块
负责语音转文本和意图分析
Python 3.11 兼容
"""

import re
from typing import Dict, Any, List, Optional
from dataclasses import dataclass

from config import (
    SPEECH_CONFIG,
    BUYING_VEGETABLES_INTENTS,
    COLORING_INTENTS,
)


@dataclass
class IntentResult:
    """意图分析结果数据类"""
    intent: str
    entities: Dict[str, Any]
    confidence: float
    raw_text: str


class SpeechProcessor:
    """语音处理器类，负责语音转文本和意图分析"""

    def __init__(self):
        """初始化语音处理器"""
        self.fuzzy_threshold = SPEECH_CONFIG.get("fuzzy_match_threshold", 3)
        self.confidence_threshold = SPEECH_CONFIG.get("confidence_threshold", 0.6)
        
        self._intent_libraries = {
            "buying_vegetables": BUYING_VEGETABLES_INTENTS,
            "coloring": COLORING_INTENTS,
        }

    def transcribe_audio(self, audio_path: str) -> str:
        """
        模拟语音转文本，包含抗噪和模糊匹配逻辑
        
        参数:
            audio_path: 音频文件路径
            
        返回:
            转换后的文本字符串
        """
        if not audio_path:
            return ""
        
        mock_transcriptions = {
            "apple.wav": "苹果",
            "pingguo.wav": "pingguo",
            "red.wav": "red",
            "hongse.wav": "hongse",
            "buy_apple.wav": "买苹果",
            "color_red.wav": "涂红色",
            "hello.wav": "你好",
            "thanks.wav": "谢谢",
        }
        
        file_name = audio_path.split("/")[-1].split("\\")[-1]
        transcribed_text = mock_transcriptions.get(file_name, file_name.replace(".wav", ""))
        
        return self._apply_noise_resistance(transcribed_text)

    def _apply_noise_resistance(self, text: str) -> str:
        """
        应用抗噪处理，模拟儿童发音不清的情况
        
        参数:
            text: 原始识别文本
            
        返回:
            处理后的文本
        """
        if not text:
            return ""
        
        text = text.lower().strip()
        text = re.sub(r'[^\w\s]', '', text)
        
        noise_patterns = {
            "嗯嗯": "",
            "那个": "",
            "这个": "",
            "啊": "",
            "呃": "",
            "嗯": "",
        }
        
        for noise, replacement in noise_patterns.items():
            text = text.replace(noise, replacement)
        
        return text.strip()

    def parse_intent(self, text: str, context: str = "buying_vegetables") -> IntentResult:
        """
        解析用户意图，基于关键词匹配
        
        参数:
            text: 用户输入文本
            context: 上下文场景（buying_vegetables 或 coloring）
            
        返回:
            IntentResult 对象，包含 intent, entities, confidence
        """
        if not text:
            return IntentResult(
                intent="unknown",
                entities={},
                confidence=0.0,
                raw_text=text
            )
        
        intent_lib = self._intent_libraries.get(context, BUYING_VEGETABLES_INTENTS)
        
        best_intent = "unknown"
        best_entities: Dict[str, Any] = {}
        best_confidence = 0.0
        
        for intent_name, intent_data in intent_lib.items():
            keywords = intent_data.get("keywords", [])
            entity_defs = intent_data.get("entities", {})
            
            keyword_score = self._calculate_keyword_score(text, keywords)
            entities = self._extract_entities(text, entity_defs)
            
            confidence = self._calculate_confidence(keyword_score, entities)
            
            if confidence > best_confidence:
                best_intent = intent_name
                best_entities = entities
                best_confidence = confidence
        
        if best_confidence < self.confidence_threshold:
            best_intent = "unknown"
            best_confidence = max(best_confidence, 0.3)
        
        return IntentResult(
            intent=best_intent,
            entities=best_entities,
            confidence=round(best_confidence, 2),
            raw_text=text
        )

    def _calculate_keyword_score(self, text: str, keywords: List[str]) -> float:
        """
        计算关键词匹配得分
        
        参数:
            text: 用户输入文本
            keywords: 关键词列表
            
        返回:
            0.0 - 1.0 的得分
        """
        if not keywords:
            return 0.0
        
        matched_count = 0
        for keyword in keywords:
            if self._fuzzy_match(text, keyword):
                matched_count += 1
        
        return min(matched_count / max(len(keywords), 1), 1.0)

    def _extract_entities(self, text: str, entity_defs: Dict[str, Dict]) -> Dict[str, Any]:
        """
        从文本中提取实体
        
        参数:
            text: 用户输入文本
            entity_defs: 实体定义字典
            
        返回:
            提取出的实体字典
        """
        entities: Dict[str, Any] = {}
        
        for entity_category, entity_values in entity_defs.items():
            matched_entities = []
            
            for entity_name, entity_aliases in entity_values.items():
                for alias in entity_aliases:
                    if self._fuzzy_match(text, alias):
                        matched_entities.append(entity_name)
                        break
            
            if matched_entities:
                entities[entity_category] = matched_entities
        
        return entities

    def _calculate_confidence(self, keyword_score: float, entities: Dict[str, Any]) -> float:
        """
        计算整体置信度
        
        参数:
            keyword_score: 关键词得分
            entities: 提取的实体
            
        返回:
            0.0 - 1.0 的置信度
        """
        entity_score = 0.0
        if entities:
            total_entity_types = len(entities)
            for entity_type, entity_list in entities.items():
                entity_score += min(len(entity_list) * 0.3, 1.0)
            entity_score = min(entity_score / total_entity_types, 1.0)
        
        confidence = (keyword_score * 0.5 + entity_score * 0.5)
        
        return confidence

    def _fuzzy_match(self, text: str, pattern: str) -> bool:
        """
        模糊匹配，基于Levenshtein距离
        
        参数:
            text: 待匹配文本
            pattern: 匹配模式
            
        返回:
            是否匹配
        """
        if pattern in text:
            return True
        
        text_lower = text.lower()
        pattern_lower = pattern.lower()
        
        if pattern_lower in text_lower:
            return True
        
        words = text_lower.split()
        for word in words:
            distance = self._levenshtein_distance(word, pattern_lower)
            if distance <= self.fuzzy_threshold:
                return True
        
        if len(pattern_lower) >= 3:
            for i in range(len(text_lower) - len(pattern_lower) + 1):
                substring = text_lower[i:i + len(pattern_lower)]
                distance = self._levenshtein_distance(substring, pattern_lower)
                if distance <= max(1, self.fuzzy_threshold - 1):
                    return True
        
        return False

    @staticmethod
    def _levenshtein_distance(s1: str, s2: str) -> int:
        """
        计算两个字符串之间的Levenshtein距离
        
        参数:
            s1: 第一个字符串
            s2: 第二个字符串
            
        返回:
            编辑距离
        """
        if len(s1) < len(s2):
            return SpeechProcessor._levenshtein_distance(s2, s1)
        
        if len(s2) == 0:
            return len(s1)
        
        previous_row = list(range(len(s2) + 1))
        
        for i, c1 in enumerate(s1):
            current_row = [i + 1]
            
            for j, c2 in enumerate(s2):
                insertions = previous_row[j + 1] + 1
                deletions = current_row[j] + 1
                substitutions = previous_row[j] + (c1 != c2)
                
                current_row.append(min(insertions, deletions, substitutions))
            
            previous_row = current_row
        
        return previous_row[-1]

    def get_supported_scenes(self) -> List[str]:
        """
        获取支持的场景列表
        
        返回:
            场景名称列表
        """
        return list(self._intent_libraries.keys())
