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

    private volatile int _mouseDevice;
    private int _keyboardDevice;

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

        // Set filters for both keyboard and mouse so we can detect the real mouse device
        Interception.SetFilter(_context, Interception.IsKeyboard, (ushort)KeyFilter.All);
        Interception.SetFilter(_context, Interception.IsMouse, (ushort)MouseFilter.All);

        _mouseDevice = 0;
        _keyboardDevice = 0;
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
                // Track the keyboard device for sending
                if (_keyboardDevice == 0)
                    _keyboardDevice = device;

                HandleKeyboardStroke(device, ref stroke);
            }
            else if (Interception.IsMouse(device) != 0)
            {
                // Track the real mouse device from actual input
                if (_mouseDevice == 0)
                    _mouseDevice = device;

                // Passthrough all mouse input
                Interception.Send(_context, device, ref stroke, 1);
            }
            else
            {
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
                ushort capturedState = (ushort)(state & ~(ushort)KeyState.Up);

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
                // Send the edit key through so Fortnite enters edit mode
<<<<<<< HEAD
                Interception.Send(_context, device, ref stroke, 1);

                // Then inject a mouse click to confirm the edit
                int mouse = _mouseDevice;
                if (mouse != 0)
=======
                _selfInjecting = true;
                Interception.Send(_context, device, ref stroke, 1);
                _selfInjecting = false;

                // Then inject a mouse click to confirm the edit
                if (_mouseDevice != 0)
>>>>>>> 0bbdc1db35eddac81d0f5f4a4ff896e7ae753388
                {
                    Thread.Sleep(5); // Small delay so the game registers the edit key first

                    var clickDown = new Stroke();
                    clickDown.Mouse.State = (ushort)MouseState.LeftDown;
                    Interception.Send(_context, mouse, ref clickDown, 1);

                    Thread.Sleep(5);

                    var clickUp = new Stroke();
                    clickUp.Mouse.State = (ushort)MouseState.LeftUp;
                    Interception.Send(_context, mouse, ref clickUp, 1);
                }
                return;
            }
            else if (isUp)
            {
<<<<<<< HEAD
                // Send the key-up through normally
=======
                // Send the key-up through so Fortnite sees the release
>>>>>>> 0bbdc1db35eddac81d0f5f4a4ff896e7ae753388
                Interception.Send(_context, device, ref stroke, 1);
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
            downStroke.Key.State = baseState;
            Interception.Send(_context, device, ref downStroke, 1);

            var upStroke = new Stroke();
            upStroke.Key.Code = code;
            upStroke.Key.State = (ushort)(baseState | (ushort)KeyState.Up);
            Interception.Send(_context, device, ref upStroke, 1);

            Thread.Sleep(1); // ~1ms between repeats
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
