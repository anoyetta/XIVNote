using System;
using aframe.Views;

namespace XIVNote.Views
{
    public interface INoteOverlay : IOverlay
    {
        Guid ID { get; }

        Note Note { get; set; }
    }
}
