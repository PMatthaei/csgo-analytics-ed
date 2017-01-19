using System;
using System.Windows;

namespace CSGO_Analytics.src.math
{
    /// <summary>
    /// Code by: https://csharpquadtree.codeplex.com/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQuadObject
    {
        Rect Bounds { get; }
        event EventHandler BoundsChanged;
    }
}