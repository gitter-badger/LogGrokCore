﻿namespace LogGrokCore.Colors.Configuration
{
    public record ColorRule
    {
        public string ForegroundColor { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string RegexString { get; set; } = string.Empty;
    }
}