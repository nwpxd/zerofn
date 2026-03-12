using System;
using System.Threading;

namespace ZeroFN;

public class InputEngine : IDisposable
{
    private IntPtr _context;
    private Thread? _mainThread;
    private Thread? _lootRepeatThread;
    private volatile bool _stopping;
    private volatile bool _disposed;

    private volatile ushort _lootScanCode;
    private volatile ushort _editScanCode;
    private volatile bool _lootEnabled;
    private volatile bool _editEnabled;
    private volatile bool _lootHeld;

    private int _mouseDevice;

    public ushort LootScanCode
    {
        get => _lootScanCode;
        set => _lootScanCode = value;
    }

    public ushort EditScanCode
    {
        get => _editScanCode;
        set => _editScanCode = value;
    }

    public bool LootEnabled
    {
        get => _lootEnabled;
        set => _lootEnabled = value;
    }

    public bool EditEnabled
    {
        get => _editEnabled;
        set => _editEnabled = value;
    }

    public InputEngine(ushort lootScanCode, ushort editScanCode, bool lootEnabled, bool editEnabled)
    {
        _lootScanCode = lootScanCode;
        _editScanCode = editScanCode;
        _lootEnabled = lootEnabled;
        _editEnabled = editEnabled;
    }

    public void Start()
    {
        _context = Interception.CreateContext();
        if (_context == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to create Interception context. Please install the Interception driver and reboot.");
        }

        // Set keyboard filter to capture all keyboard events
        Interception.SetFilter(_context, Interception.IsKeyboard, (ushort)KeyFilter.All);

        // Find first mouse device (devices 11-20 are mouse devices in Interception)
        _mouseDevice = 0;
        for (int i = 11; i <= 20; i++)
        {
            if (Interception.IsMouse(i) != 0)
            {
                _mouseDevice = i;
                break;
            }
        }

        _stopping = false;
        _mainThread = new Thread(MainLoop) { IsBackground = true, Name = "InputEngine" };
        _mainThread.Start();
    }

    public void Stop()
    {
        _stopping = true;
        _lootHeld = false;

        _lootRepeatThread?.Join(500);
        _mainThread?.Join(500);

        if (_context != IntPtr.Zero)
        {
            Interception.DestroyContext(_context);
            _context = IntPtr.Zero;
        }
    }

    private void MainLoop()
    {
        while (!_stopping)
        {
            int device = Interception.WaitWithTimeout(_context, 10);
            if (device == 0)
                continue;

            var stroke = new Stroke();
            if (Interception.Receive(_context, device, ref stroke, 1) <= 0)
                continue;

            if (Interception.IsKeyboard(device) != 0)
            {
                HandleKeyboardStroke(device, ref stroke);
            }
            else
            {
                // Passthrough non-keyboard strokes
                Interception.Send(_context, device, ref stroke, 1);
            }
        }
    }

    private void HandleKeyboardStroke(int device, ref Stroke stroke)
    {
        ushort code = stroke.Key.Code;
        ushort state = stroke.Key.State;
        bool isDown = (state & (ushort)KeyState.Up) == 0;
        bool isUp = (state & (ushort)KeyState.Up) != 0;

        // Check Loot key
        if (code == _lootScanCode && _lootEnabled)
        {
            if (isDown && !_lootHeld)
            {
                _lootHeld = true;
                int capturedDevice = device;
                ushort capturedCode = code;
                ushort capturedState = (ushort)(state & ~(ushort)KeyState.Up); // Ensure down state base flags

                _lootRepeatThread = new Thread(() => LootRepeatLoop(capturedDevice, capturedCode, capturedState))
                {
                    IsBackground = true,
                    Name = "LootRepeat"
                };
                _lootRepeatThread.Start();
                return; // Consume the original keystroke
            }
            else if (isUp)
            {
                _lootHeld = false;
                // Send the key-up through
                Interception.Send(_context, device, ref stroke, 1);
                return;
            }
            else if (isDown && _lootHeld)
            {
                // OS auto-repeat key-down while already held — consume it
                return;
            }
        }

        // Check Edit key
        if (code == _editScanCode && _editEnabled)
        {
            if (isDown)
            {
                // Consume the keystroke, inject one mouse click
                if (_mouseDevice != 0)
                {
                    var clickDown = new Stroke();
                    clickDown.Mouse.State = (ushort)MouseState.LeftDown;
                    clickDown.Mouse.Flags = 0;
                    clickDown.Mouse.Rolling = 0;
                    clickDown.Mouse.X = 0;
                    clickDown.Mouse.Y = 0;
                    clickDown.Mouse.Information = 0;
                    Interception.Send(_context, _mouseDevice, ref clickDown, 1);

                    var clickUp = new Stroke();
                    clickUp.Mouse.State = (ushort)MouseState.LeftUp;
                    clickUp.Mouse.Flags = 0;
                    clickUp.Mouse.Rolling = 0;
                    clickUp.Mouse.X = 0;
                    clickUp.Mouse.Y = 0;
                    clickUp.Mouse.Information = 0;
                    Interception.Send(_context, _mouseDevice, ref clickUp, 1);
                }
                return;
            }
            else if (isUp)
            {
                // Consume silently
                return;
            }
        }

        // Passthrough all other keys
        Interception.Send(_context, device, ref stroke, 1);
    }

    private void LootRepeatLoop(int device, ushort code, ushort baseState)
    {
        while (_lootHeld && !_stopping)
        {
            var downStroke = new Stroke();
            downStroke.Key.Code = code;
            downStroke.Key.State = baseState; // Down
            Interception.Send(_context, device, ref downStroke, 1);

            var upStroke = new Stroke();
            upStroke.Key.Code = code;
            upStroke.Key.State = (ushort)(baseState | (ushort)KeyState.Up);
            Interception.Send(_context, device, ref upStroke, 1);

            Thread.SpinWait(1);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Stop();
        }
    }
}
