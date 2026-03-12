using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroFN;

public class ToggleSwitch : UserControl
{
    private bool _checked;
    private float _knobX;
    private readonly System.Windows.Forms.Timer _animTimer;
    private const int TrackWidth = 50;
    private const int TrackHeight = 24;
    private const int KnobSize = 18;
    private const int KnobPadding = 3;
    private const float AnimStep = 4f;

    private static readonly Color OffTrackColor = Color.FromArgb(60, 60, 70);
    private static readonly Color OffKnobColor = Color.FromArgb(120, 120, 130);
    private static readonly Color OnTrackColor = Color.FromArgb(0, 210, 255);
    private static readonly Color OnKnobColor = Color.White;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked != value)
            {
                _checked = value;
                _animTimer.Start();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(TrackWidth, TrackHeight);
        _knobX = KnobPadding;

        _animTimer = new System.Windows.Forms.Timer { Interval = 10 };
        _animTimer.Tick += OnAnimTick;

        Click += (_, _) => Checked = !Checked;
    }

    private float TargetX => _checked ? TrackWidth - KnobSize - KnobPadding : KnobPadding;

    private void OnAnimTick(object? sender, EventArgs e)
    {
        float target = TargetX;
        if (Math.Abs(_knobX - target) < AnimStep)
        {
            _knobX = target;
            _animTimer.Stop();
        }
        else
        {
            _knobX += _knobX < target ? AnimStep : -AnimStep;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float t = (_knobX - KnobPadding) / (TrackWidth - KnobSize - KnobPadding * 2);
        t = Math.Clamp(t, 0f, 1f);

        var trackColor = InterpolateColor(OffTrackColor, OnTrackColor, t);
        var knobColor = InterpolateColor(OffKnobColor, OnKnobColor, t);

        // Track
        using var trackBrush = new SolidBrush(trackColor);
        var trackRect = new RectangleF(0, 0, TrackWidth, TrackHeight);
        using var trackPath = RoundedRect(trackRect, TrackHeight / 2f);
        g.FillPath(trackBrush, trackPath);

        // Knob
        using var knobBrush = new SolidBrush(knobColor);
        float knobY = (TrackHeight - KnobSize) / 2f;
        g.FillEllipse(knobBrush, _knobX, knobY, KnobSize, KnobSize);
    }

    private static Color InterpolateColor(Color a, Color b, float t)
    {
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    private static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _animTimer.Dispose();
        base.Dispose(disposing);
    }
}
