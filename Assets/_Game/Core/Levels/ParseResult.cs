using System.Collections.Generic;

namespace Game.Core.Levels
{
    public sealed class ParseResult
    {
        private ParseResult(LevelData level, IReadOnlyList<string> errors)
        {
            Level = level;
            Errors = errors;
        }

        public bool Success => Level != null;
        public LevelData Level { get; }                // null when parsing failed
        public IReadOnlyList<string> Errors { get; }   // empty when parsing succeeded

        public static ParseResult Ok(LevelData level) => new ParseResult(level, new string[0]);
        public static ParseResult Failed(IReadOnlyList<string> errors) => new ParseResult(null, errors);
    }
}
