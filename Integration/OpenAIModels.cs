using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RhinoAI.Integration
{
    // This file contains models for OpenAI API requests and responses.
    #region Chat Completions

    public class OpenAIChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; }
    }

    public class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice> Choices { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage Message { get; set; }
    }

    #endregion

    #region Vision Completions

    public class OpenAIVisionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenAIVisionMessage> Messages { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class OpenAIVisionMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<OpenAIVisionContent> Content { get; set; }
    }

    public class OpenAIVisionContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Text { get; set; }

        [JsonPropertyName("image_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenAIVisionImageUrl ImageUrl { get; set; }
    }

    public class OpenAIVisionImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
    
    // Using the same OpenAIChatResponse for Vision API as the structure is similar

    #endregion

    #region Common

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    #endregion
} 