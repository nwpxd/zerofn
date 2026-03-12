using System;
using System.Runtime.InteropServices;

namespace ZeroFN;

[Flags]
public enum KeyState : ushort
{
    Down = 0x00,
    Up = 0x01,
    E0 = 0x02,
    E1 = 0x04,
}

[Flags]
public enum MouseState : ushort
{
    LeftDown = 0x001,
    LeftUp = 0x002,
    RightDown = 0x004,
    RightUp = 0x008,
    MiddleDown = 0x010,
    MiddleUp = 0x020,
}

[Flags]
public enum KeyFilter : ushort
{
    None = 0x0000,
    All = 0xFFFF,
    KeyDown = 0x01,
    KeyUp = 0x02,
    E0 = 0x04,
    E1 = 0x08,
}

[Flags]
public enum MouseFilter : ushort
{
    None = 0x0000,
    All = 0xFFFF,
    LeftDown = 0x01,
    LeftUp = 0x02,
    RightDown = 0x04,
    RightUp = 0x08,
    MiddleDown = 0x10,
    MiddleUp = 0x20,
    Move = 0x1000,
}

[StructLayout(LayoutKind.Sequential)]
public struct KeyStroke
{
    public ushort Code;
    public ushort State;
    public uint Information;
}

[StructLayout(LayoutKind.Sequential)]
public struct MouseStroke
{
    public ushort State;
    public ushort Flags;
    public short Rolling;
    public int X;
    public int Y;
    public uint Information;
}

[StructLayout(LayoutKind.Explicit)]
public struct Stroke
{
    [FieldOffset(0)] public KeyStroke Key;
    [FieldOffset(0)] public MouseStroke Mouse;
}

public static class Interception
{
    private const string DllName = "interception.dll";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int Predicate(int device);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_create_context")]
    public static extern IntPtr CreateContext();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_destroy_context")]
    public static extern void DestroyContext(IntPtr context);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_set_filter")]
    public static extern void SetFilter(IntPtr context, Predicate predicate, ushort filter);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_wait_with_timeout")]
    public static extern int WaitWithTimeout(IntPtr context, ulong milliseconds);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_receive")]
    public static extern int Receive(IntPtr context, int device, ref Stroke stroke, uint nstroke);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_send")]
    public static extern int Send(IntPtr context, int device, ref Stroke stroke, uint nstroke);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_is_keyboard")]
    public static extern int IsKeyboard(int device);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_is_mouse")]
    public static extern int IsMouse(int device);
}
