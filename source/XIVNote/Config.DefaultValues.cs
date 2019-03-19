using System.Collections.Generic;

namespace XIVNote
{
    public partial class Config
    {
        public static readonly double DefaultWidth = 380;
        public static readonly double DefaultHeight = 640;

        public override Dictionary<string, object> DefaultValues => new Dictionary<string, object>()
        {
            { nameof(Scale), 1.0d },
            { nameof(X), 200 },
            { nameof(Y), 100 },
            { nameof(W), DefaultWidth },
            { nameof(H), DefaultHeight },
            { nameof(IsStartupWithWindows), false },
            { nameof(IsMinimizeStartup), false },
        };
    }
}
