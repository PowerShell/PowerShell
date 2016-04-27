/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
#if CORECLR
using System.Runtime.InteropServices;
#endif

namespace Microsoft.PowerShell
{
    internal static class Keys
    {
        static Keys()
        {
#if CORECLR
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Enter = new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, false, false);
            }
            else
            {
                Enter = new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false);
            }
#else
            Enter = new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false);
#endif
        }

        public static ConsoleKeyInfo A = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        public static ConsoleKeyInfo B = new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false);
        public static ConsoleKeyInfo C = new ConsoleKeyInfo('c', ConsoleKey.C, false, false, false);
        public static ConsoleKeyInfo D = new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false);
        public static ConsoleKeyInfo E = new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false);
        public static ConsoleKeyInfo F = new ConsoleKeyInfo('f', ConsoleKey.F, false, false, false);
        public static ConsoleKeyInfo G = new ConsoleKeyInfo('g', ConsoleKey.G, false, false, false);
        public static ConsoleKeyInfo H = new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false);
        public static ConsoleKeyInfo I = new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false);
        public static ConsoleKeyInfo J = new ConsoleKeyInfo('j', ConsoleKey.J, false, false, false);
        public static ConsoleKeyInfo K = new ConsoleKeyInfo('k', ConsoleKey.K, false, false, false);
        public static ConsoleKeyInfo L = new ConsoleKeyInfo('l', ConsoleKey.L, false, false, false);
        public static ConsoleKeyInfo M = new ConsoleKeyInfo('m', ConsoleKey.M, false, false, false);
        public static ConsoleKeyInfo N = new ConsoleKeyInfo('n', ConsoleKey.N, false, false, false);
        public static ConsoleKeyInfo O = new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false);
        public static ConsoleKeyInfo P = new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false);
        public static ConsoleKeyInfo Q = new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false);
        public static ConsoleKeyInfo R = new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false);
        public static ConsoleKeyInfo S = new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false);
        public static ConsoleKeyInfo T = new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false);
        public static ConsoleKeyInfo U = new ConsoleKeyInfo('u', ConsoleKey.U, false, false, false);
        public static ConsoleKeyInfo V = new ConsoleKeyInfo('v', ConsoleKey.V, false, false, false);
        public static ConsoleKeyInfo W = new ConsoleKeyInfo('w', ConsoleKey.W, false, false, false);
        public static ConsoleKeyInfo X = new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false);
        public static ConsoleKeyInfo Y = new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false);
        public static ConsoleKeyInfo Z = new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false);
        public static ConsoleKeyInfo ucA = new ConsoleKeyInfo('A', ConsoleKey.A, true, false, false);
        public static ConsoleKeyInfo ucB = new ConsoleKeyInfo('B', ConsoleKey.B, true, false, false);
        public static ConsoleKeyInfo ucC = new ConsoleKeyInfo('C', ConsoleKey.C, true, false, false);
        public static ConsoleKeyInfo ucD = new ConsoleKeyInfo('D', ConsoleKey.D, true, false, false);
        public static ConsoleKeyInfo ucE = new ConsoleKeyInfo('E', ConsoleKey.E, true, false, false);
        public static ConsoleKeyInfo ucF = new ConsoleKeyInfo('F', ConsoleKey.F, true, false, false);
        public static ConsoleKeyInfo ucG = new ConsoleKeyInfo('G', ConsoleKey.G, true, false, false);
        public static ConsoleKeyInfo ucH = new ConsoleKeyInfo('H', ConsoleKey.H, true, false, false);
        public static ConsoleKeyInfo ucI = new ConsoleKeyInfo('I', ConsoleKey.I, true, false, false);
        public static ConsoleKeyInfo ucJ = new ConsoleKeyInfo('J', ConsoleKey.J, true, false, false);
        public static ConsoleKeyInfo ucK = new ConsoleKeyInfo('K', ConsoleKey.K, true, false, false);
        public static ConsoleKeyInfo ucL = new ConsoleKeyInfo('L', ConsoleKey.L, true, false, false);
        public static ConsoleKeyInfo ucM = new ConsoleKeyInfo('M', ConsoleKey.M, true, false, false);
        public static ConsoleKeyInfo ucN = new ConsoleKeyInfo('N', ConsoleKey.N, true, false, false);
        public static ConsoleKeyInfo ucO = new ConsoleKeyInfo('O', ConsoleKey.O, true, false, false);
        public static ConsoleKeyInfo ucP = new ConsoleKeyInfo('P', ConsoleKey.P, true, false, false);
        public static ConsoleKeyInfo ucQ = new ConsoleKeyInfo('Q', ConsoleKey.Q, true, false, false);
        public static ConsoleKeyInfo ucR = new ConsoleKeyInfo('R', ConsoleKey.R, true, false, false);
        public static ConsoleKeyInfo ucS = new ConsoleKeyInfo('S', ConsoleKey.S, true, false, false);
        public static ConsoleKeyInfo ucT = new ConsoleKeyInfo('T', ConsoleKey.T, true, false, false);
        public static ConsoleKeyInfo ucU = new ConsoleKeyInfo('U', ConsoleKey.U, true, false, false);
        public static ConsoleKeyInfo ucV = new ConsoleKeyInfo('V', ConsoleKey.V, true, false, false);
        public static ConsoleKeyInfo ucW = new ConsoleKeyInfo('W', ConsoleKey.W, true, false, false);
        public static ConsoleKeyInfo ucX = new ConsoleKeyInfo('X', ConsoleKey.X, true, false, false);
        public static ConsoleKeyInfo ucY = new ConsoleKeyInfo('Y', ConsoleKey.Y, true, false, false);
        public static ConsoleKeyInfo ucZ = new ConsoleKeyInfo('Z', ConsoleKey.Z, true, false, false);

        public static ConsoleKeyInfo _0 = new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false);
        public static ConsoleKeyInfo _1 = new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false);
        public static ConsoleKeyInfo _2 = new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false);
        public static ConsoleKeyInfo _3 = new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false);
        public static ConsoleKeyInfo _4 = new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false);
        public static ConsoleKeyInfo _5 = new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false);
        public static ConsoleKeyInfo _6 = new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false);
        public static ConsoleKeyInfo _7 = new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false);
        public static ConsoleKeyInfo _8 = new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false);
        public static ConsoleKeyInfo _9 = new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false);

        public static ConsoleKeyInfo RParen    = new ConsoleKeyInfo(')', ConsoleKey.D0, true, false, false);
        public static ConsoleKeyInfo Bang      = new ConsoleKeyInfo('!', ConsoleKey.D1, true, false, false);
        public static ConsoleKeyInfo At        = new ConsoleKeyInfo('@', ConsoleKey.D2, true, false, false);
        public static ConsoleKeyInfo Pound     = new ConsoleKeyInfo('#', ConsoleKey.D3, true, false, false);
        public static ConsoleKeyInfo Dollar    = new ConsoleKeyInfo('$', ConsoleKey.D4, true, false, false);
        public static ConsoleKeyInfo Percent   = new ConsoleKeyInfo('%', ConsoleKey.D5, true, false, false);
        public static ConsoleKeyInfo Uphat     = new ConsoleKeyInfo('^', ConsoleKey.D6, true, false, false);
        public static ConsoleKeyInfo Ampersand = new ConsoleKeyInfo('&', ConsoleKey.D7, true, false, false);
        public static ConsoleKeyInfo Star      = new ConsoleKeyInfo('*', ConsoleKey.D8, true, false, false);
        public static ConsoleKeyInfo LParen    = new ConsoleKeyInfo('(', ConsoleKey.D9, true, false, false);

        public static ConsoleKeyInfo Colon       = new ConsoleKeyInfo(':', ConsoleKey.Oem1, true, false, false);
        public static ConsoleKeyInfo Semicolon   = new ConsoleKeyInfo(';', ConsoleKey.Oem1, false, false, false);
        public static ConsoleKeyInfo Question    = new ConsoleKeyInfo('?', ConsoleKey.Oem2, true, false, false);
        public static ConsoleKeyInfo Slash       = new ConsoleKeyInfo('/', ConsoleKey.Oem2, false, false, false);
        public static ConsoleKeyInfo Tilde       = new ConsoleKeyInfo('~', ConsoleKey.Oem3, true, false, false);
        public static ConsoleKeyInfo Backtick    = new ConsoleKeyInfo('`', ConsoleKey.Oem3, false, false, false);
        public static ConsoleKeyInfo LCurly      = new ConsoleKeyInfo('{', ConsoleKey.Oem4, true, false, false);
        public static ConsoleKeyInfo LBracket    = new ConsoleKeyInfo('[', ConsoleKey.Oem4, false, false, false);
        public static ConsoleKeyInfo Pipe        = new ConsoleKeyInfo('|', ConsoleKey.Oem5, true, false, false);
        public static ConsoleKeyInfo Backslash   = new ConsoleKeyInfo('\\', ConsoleKey.Oem5, false, false, false);
        public static ConsoleKeyInfo RCurly      = new ConsoleKeyInfo('}', ConsoleKey.Oem6, true, false, false);
        public static ConsoleKeyInfo RBracket    = new ConsoleKeyInfo(']', ConsoleKey.Oem6, false, false, false);
        public static ConsoleKeyInfo SQuote      = new ConsoleKeyInfo('\'', ConsoleKey.Oem7, false, false, false);
        public static ConsoleKeyInfo DQuote      = new ConsoleKeyInfo('"', ConsoleKey.Oem7, true, false, false);
        public static ConsoleKeyInfo LessThan    = new ConsoleKeyInfo('<', ConsoleKey.OemComma, true, false, false);
        public static ConsoleKeyInfo Comma       = new ConsoleKeyInfo(',', ConsoleKey.OemComma, false, false, false);
        public static ConsoleKeyInfo GreaterThan = new ConsoleKeyInfo('>', ConsoleKey.OemPeriod, true, false, false);
        public static ConsoleKeyInfo Period      = new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false);
        public static ConsoleKeyInfo Underbar    = new ConsoleKeyInfo('_', ConsoleKey.OemMinus, true, false, false);
        public static ConsoleKeyInfo Minus       = new ConsoleKeyInfo('-', ConsoleKey.OemMinus, false, false, false);
        public static ConsoleKeyInfo AltMinus    = new ConsoleKeyInfo('-', ConsoleKey.OemMinus, false, true, false);
        public static ConsoleKeyInfo Plus        = new ConsoleKeyInfo('+', ConsoleKey.OemPlus, true, false, false);
        new
        public static ConsoleKeyInfo Equals      = new ConsoleKeyInfo('=', ConsoleKey.OemPlus, false, false, false);

        public static ConsoleKeyInfo CtrlAt       = new ConsoleKeyInfo((char)0, ConsoleKey.D2, true, false, true);
        public static ConsoleKeyInfo AltUnderbar = new ConsoleKeyInfo('_', ConsoleKey.OemMinus, true, true, false);
        public static ConsoleKeyInfo CtrlUnderbar = new ConsoleKeyInfo((char)31, ConsoleKey.OemMinus, true, false, true);
        public static ConsoleKeyInfo AltEquals    = new ConsoleKeyInfo('=', ConsoleKey.OemPlus, false, true, false);
        public static ConsoleKeyInfo Space        = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        // Useless because it's caught by the console to bring up the system menu.
        public static ConsoleKeyInfo AltSpace    = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, true, false);
        public static ConsoleKeyInfo CtrlSpace   = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, true);
        public static ConsoleKeyInfo AltLess     = new ConsoleKeyInfo('<', ConsoleKey.OemComma, true, true, false);
        public static ConsoleKeyInfo AltGreater  = new ConsoleKeyInfo('>', ConsoleKey.OemPeriod, true, true, false);
        public static ConsoleKeyInfo CtrlRBracket = new ConsoleKeyInfo((char)29, ConsoleKey.Oem6, false, false, true);
        public static ConsoleKeyInfo AltCtrlRBracket = new ConsoleKeyInfo((char)0, ConsoleKey.Oem6, false, true, true);
        public static ConsoleKeyInfo AltPeriod    = new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, true, false);
        public static ConsoleKeyInfo CtrlAltQuestion  = new ConsoleKeyInfo((char)0, ConsoleKey.Oem2, true, true, true);
        public static ConsoleKeyInfo AltQuestion  = new ConsoleKeyInfo('?', ConsoleKey.Oem2, true, true, false);

        public static ConsoleKeyInfo Alt0 = new ConsoleKeyInfo('0', ConsoleKey.D0, false, true, false);
        public static ConsoleKeyInfo Alt1 = new ConsoleKeyInfo('1', ConsoleKey.D1, false, true, false);
        public static ConsoleKeyInfo Alt2 = new ConsoleKeyInfo('2', ConsoleKey.D2, false, true, false);
        public static ConsoleKeyInfo Alt3 = new ConsoleKeyInfo('3', ConsoleKey.D3, false, true, false);
        public static ConsoleKeyInfo Alt4 = new ConsoleKeyInfo('4', ConsoleKey.D4, false, true, false);
        public static ConsoleKeyInfo Alt5 = new ConsoleKeyInfo('5', ConsoleKey.D5, false, true, false);
        public static ConsoleKeyInfo Alt6 = new ConsoleKeyInfo('6', ConsoleKey.D6, false, true, false);
        public static ConsoleKeyInfo Alt7 = new ConsoleKeyInfo('7', ConsoleKey.D7, false, true, false);
        public static ConsoleKeyInfo Alt8 = new ConsoleKeyInfo('8', ConsoleKey.D8, false, true, false);
        public static ConsoleKeyInfo Alt9 = new ConsoleKeyInfo('9', ConsoleKey.D9, false, true, false);

        public static ConsoleKeyInfo AltA = new ConsoleKeyInfo((char)97, ConsoleKey.A, false, true, false);
        public static ConsoleKeyInfo AltB = new ConsoleKeyInfo((char)98, ConsoleKey.B, false, true, false);
        public static ConsoleKeyInfo AltC = new ConsoleKeyInfo((char)99, ConsoleKey.C, false, true, false);
        public static ConsoleKeyInfo AltD = new ConsoleKeyInfo((char)100, ConsoleKey.D, false, true, false);
        public static ConsoleKeyInfo AltE = new ConsoleKeyInfo((char)101, ConsoleKey.E, false, true, false);
        public static ConsoleKeyInfo AltF = new ConsoleKeyInfo((char)102, ConsoleKey.F, false, true, false);
        public static ConsoleKeyInfo AltG = new ConsoleKeyInfo((char)103, ConsoleKey.G, false, true, false);
        public static ConsoleKeyInfo AltH = new ConsoleKeyInfo((char)104, ConsoleKey.H, false, true, false);
        public static ConsoleKeyInfo AltI = new ConsoleKeyInfo((char)105, ConsoleKey.I, false, true, false);
        public static ConsoleKeyInfo AltJ = new ConsoleKeyInfo((char)106, ConsoleKey.J, false, true, false);
        public static ConsoleKeyInfo AltK = new ConsoleKeyInfo((char)107, ConsoleKey.K, false, true, false);
        public static ConsoleKeyInfo AltL = new ConsoleKeyInfo((char)108, ConsoleKey.L, false, true, false);
        public static ConsoleKeyInfo AltM = new ConsoleKeyInfo((char)109, ConsoleKey.M, false, true, false);
        public static ConsoleKeyInfo AltN = new ConsoleKeyInfo((char)110, ConsoleKey.N, false, true, false);
        public static ConsoleKeyInfo AltO = new ConsoleKeyInfo((char)111, ConsoleKey.O, false, true, false);
        public static ConsoleKeyInfo AltP = new ConsoleKeyInfo((char)112, ConsoleKey.P, false, true, false);
        public static ConsoleKeyInfo AltQ = new ConsoleKeyInfo((char)113, ConsoleKey.Q, false, true, false);
        public static ConsoleKeyInfo AltR = new ConsoleKeyInfo((char)114, ConsoleKey.R, false, true, false);
        public static ConsoleKeyInfo AltS = new ConsoleKeyInfo((char)115, ConsoleKey.S, false, true, false);
        public static ConsoleKeyInfo AltT = new ConsoleKeyInfo((char)116, ConsoleKey.T, false, true, false);
        public static ConsoleKeyInfo AltU = new ConsoleKeyInfo((char)117, ConsoleKey.U, false, true, false);
        public static ConsoleKeyInfo AltV = new ConsoleKeyInfo((char)118, ConsoleKey.V, false, true, false);
        public static ConsoleKeyInfo AltW = new ConsoleKeyInfo((char)119, ConsoleKey.W, false, true, false);
        public static ConsoleKeyInfo AltX = new ConsoleKeyInfo((char)120, ConsoleKey.X, false, true, false);
        public static ConsoleKeyInfo AltY = new ConsoleKeyInfo((char)121, ConsoleKey.Y, false, true, false);
        public static ConsoleKeyInfo AltZ = new ConsoleKeyInfo((char)122, ConsoleKey.Z, false, true, false);

        public static ConsoleKeyInfo CtrlA = new ConsoleKeyInfo((char)1, ConsoleKey.A, false, false, true);
        public static ConsoleKeyInfo CtrlB = new ConsoleKeyInfo((char)2, ConsoleKey.B, false, false, true);
        public static ConsoleKeyInfo CtrlC = new ConsoleKeyInfo((char)3, ConsoleKey.C, false, false, true);
        public static ConsoleKeyInfo CtrlD = new ConsoleKeyInfo((char)4, ConsoleKey.D, false, false, true);
        public static ConsoleKeyInfo CtrlE = new ConsoleKeyInfo((char)5, ConsoleKey.E, false, false, true);
        public static ConsoleKeyInfo CtrlF = new ConsoleKeyInfo((char)6, ConsoleKey.F, false, false, true);
        public static ConsoleKeyInfo CtrlG = new ConsoleKeyInfo((char)7, ConsoleKey.G, false, false, true);
        public static ConsoleKeyInfo CtrlH = new ConsoleKeyInfo((char)8, ConsoleKey.H, false, false, true);
        public static ConsoleKeyInfo CtrlI = new ConsoleKeyInfo((char)9, ConsoleKey.I, false, false, true);
        public static ConsoleKeyInfo CtrlJ = new ConsoleKeyInfo((char)10, ConsoleKey.J, false, false, true);
        public static ConsoleKeyInfo CtrlK = new ConsoleKeyInfo((char)11, ConsoleKey.K, false, false, true);
        public static ConsoleKeyInfo CtrlL = new ConsoleKeyInfo((char)12, ConsoleKey.L, false, false, true);
        public static ConsoleKeyInfo CtrlM = new ConsoleKeyInfo((char)13, ConsoleKey.M, false, false, true);
        public static ConsoleKeyInfo CtrlN = new ConsoleKeyInfo((char)14, ConsoleKey.N, false, false, true);
        public static ConsoleKeyInfo CtrlO = new ConsoleKeyInfo((char)15, ConsoleKey.O, false, false, true);
        public static ConsoleKeyInfo CtrlP = new ConsoleKeyInfo((char)16, ConsoleKey.P, false, false, true);
        public static ConsoleKeyInfo CtrlQ = new ConsoleKeyInfo((char)17, ConsoleKey.Q, false, false, true);
        public static ConsoleKeyInfo CtrlR = new ConsoleKeyInfo((char)18, ConsoleKey.R, false, false, true);
        public static ConsoleKeyInfo CtrlS = new ConsoleKeyInfo((char)19, ConsoleKey.S, false, false, true);
        public static ConsoleKeyInfo CtrlT = new ConsoleKeyInfo((char)20, ConsoleKey.T, false, false, true);
        public static ConsoleKeyInfo CtrlU = new ConsoleKeyInfo((char)21, ConsoleKey.U, false, false, true);
        public static ConsoleKeyInfo CtrlV = new ConsoleKeyInfo((char)22, ConsoleKey.V, false, false, true);
        public static ConsoleKeyInfo CtrlW = new ConsoleKeyInfo((char)23, ConsoleKey.W, false, false, true);
        public static ConsoleKeyInfo CtrlX = new ConsoleKeyInfo((char)24, ConsoleKey.X, false, false, true);
        public static ConsoleKeyInfo CtrlY = new ConsoleKeyInfo((char)25, ConsoleKey.Y, false, false, true);
        public static ConsoleKeyInfo CtrlZ = new ConsoleKeyInfo((char)26, ConsoleKey.Z, false, false, true);

        public static ConsoleKeyInfo CtrlShiftC = new ConsoleKeyInfo((char)3, ConsoleKey.C, true, false, true);

        public static ConsoleKeyInfo AltShiftB = new ConsoleKeyInfo('B', ConsoleKey.B, true, true, false);
        public static ConsoleKeyInfo AltShiftF = new ConsoleKeyInfo('F', ConsoleKey.F, true, true, false);

        public static ConsoleKeyInfo AltCtrlY = new ConsoleKeyInfo((char)0, ConsoleKey.Y, false, true, true);

        public static ConsoleKeyInfo Backspace    = new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false);
        public static ConsoleKeyInfo CtrlBackspace = new ConsoleKeyInfo((char)0x7f, ConsoleKey.Backspace, false, false, true);
        public static ConsoleKeyInfo AltBackspace = new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false);
        public static ConsoleKeyInfo Delete       = new ConsoleKeyInfo((char)0, ConsoleKey.Delete, false, false, false);
        public static ConsoleKeyInfo CtrlDelete   = new ConsoleKeyInfo((char)0, ConsoleKey.Delete, false, false, true);
        public static ConsoleKeyInfo DownArrow    = new ConsoleKeyInfo((char)0, ConsoleKey.DownArrow, false, false, false);
        public static ConsoleKeyInfo End          = new ConsoleKeyInfo((char)0, ConsoleKey.End, false, false, false);
        public static ConsoleKeyInfo CtrlEnd      = new ConsoleKeyInfo((char)0, ConsoleKey.End, false, false, true);
        public static ConsoleKeyInfo ShiftEnd     = new ConsoleKeyInfo((char)0, ConsoleKey.End, true, false, false);
        public static ConsoleKeyInfo Enter; 
        public static ConsoleKeyInfo Escape       = new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false);
        public static ConsoleKeyInfo Home         = new ConsoleKeyInfo((char)0, ConsoleKey.Home, false, false, false);
        public static ConsoleKeyInfo CtrlHome     = new ConsoleKeyInfo((char)0, ConsoleKey.Home, false, false, true);
        public static ConsoleKeyInfo ShiftHome    = new ConsoleKeyInfo((char)0, ConsoleKey.Home, true, false, false);
        public static ConsoleKeyInfo LeftArrow    = new ConsoleKeyInfo((char)0, ConsoleKey.LeftArrow, false, false, false);
        public static ConsoleKeyInfo RightArrow   = new ConsoleKeyInfo((char)0, ConsoleKey.RightArrow, false, false, false);
        public static ConsoleKeyInfo Tab          = new ConsoleKeyInfo((char)9, ConsoleKey.Tab, false, false, false);
        public static ConsoleKeyInfo UpArrow      = new ConsoleKeyInfo((char)0, ConsoleKey.UpArrow, false, false, false);
        public static ConsoleKeyInfo PageUp       = new ConsoleKeyInfo((char)0, ConsoleKey.PageUp, false, false, false);
        public static ConsoleKeyInfo PageDown     = new ConsoleKeyInfo((char)0, ConsoleKey.PageDown, false, false, false);
        public static ConsoleKeyInfo ShiftPageUp   = new ConsoleKeyInfo((char)0, ConsoleKey.PageUp, true, false, false);
        public static ConsoleKeyInfo ShiftPageDown = new ConsoleKeyInfo((char)0, ConsoleKey.PageDown, true, false, false);
        public static ConsoleKeyInfo CtrlPageUp    = new ConsoleKeyInfo((char)0, ConsoleKey.PageUp, false, false, true);
        public static ConsoleKeyInfo CtrlPageDown  = new ConsoleKeyInfo((char)0, ConsoleKey.PageDown, false, false, true);
        public static ConsoleKeyInfo AltPageUp     = new ConsoleKeyInfo((char)0, ConsoleKey.PageUp, false, true, false);
        public static ConsoleKeyInfo AltPageDown   = new ConsoleKeyInfo((char)0, ConsoleKey.PageDown, false, true, false);

        public static ConsoleKeyInfo ShiftLeftArrow  = new ConsoleKeyInfo((char)0, ConsoleKey.LeftArrow, true, false, false);
        public static ConsoleKeyInfo ShiftRightArrow = new ConsoleKeyInfo((char)0, ConsoleKey.RightArrow, true, false, false);
        public static ConsoleKeyInfo CtrlLeftArrow  = new ConsoleKeyInfo((char)0, ConsoleKey.LeftArrow, false, false, true);
        public static ConsoleKeyInfo CtrlRightArrow = new ConsoleKeyInfo((char)0, ConsoleKey.RightArrow, false, false, true);
        public static ConsoleKeyInfo ShiftCtrlLeftArrow  = new ConsoleKeyInfo((char)0, ConsoleKey.LeftArrow, true, false, true);
        public static ConsoleKeyInfo ShiftCtrlRightArrow = new ConsoleKeyInfo((char)0, ConsoleKey.RightArrow, true, false, true);

        public static ConsoleKeyInfo ShiftTab = new ConsoleKeyInfo((char)9, ConsoleKey.Tab, true, false, false);

        public static ConsoleKeyInfo CtrlEnter      = new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, false, true);
        public static ConsoleKeyInfo CtrlShiftEnter = new ConsoleKeyInfo((char)0,  ConsoleKey.Enter, true, false, true);
        public static ConsoleKeyInfo ShiftEnter     = new ConsoleKeyInfo((char)13, ConsoleKey.Enter, true, false, false);

        public static ConsoleKeyInfo F1 = new ConsoleKeyInfo((char)0, ConsoleKey.F1, false, false, false);
        public static ConsoleKeyInfo F2 = new ConsoleKeyInfo((char)0, ConsoleKey.F2, false, false, false);
        public static ConsoleKeyInfo F3 = new ConsoleKeyInfo((char)0, ConsoleKey.F3, false, false, false);
        public static ConsoleKeyInfo F4 = new ConsoleKeyInfo((char)0, ConsoleKey.F4, false, false, false);
        public static ConsoleKeyInfo F5 = new ConsoleKeyInfo((char)0, ConsoleKey.F5, false, false, false);
        public static ConsoleKeyInfo F6 = new ConsoleKeyInfo((char)0, ConsoleKey.F6, false, false, false);
        public static ConsoleKeyInfo F7 = new ConsoleKeyInfo((char)0, ConsoleKey.F7, false, false, false);
        public static ConsoleKeyInfo F8 = new ConsoleKeyInfo((char)0, ConsoleKey.F8, false, false, false);
        public static ConsoleKeyInfo F9 = new ConsoleKeyInfo((char)0, ConsoleKey.F9, false, false, false);
        public static ConsoleKeyInfo Fl0 = new ConsoleKeyInfo((char)0, ConsoleKey.F10, false, false, false);
        public static ConsoleKeyInfo F11 = new ConsoleKeyInfo((char)0, ConsoleKey.F11, false, false, false);
        public static ConsoleKeyInfo F12 = new ConsoleKeyInfo((char)0, ConsoleKey.F12, false, false, false);
        public static ConsoleKeyInfo F13 = new ConsoleKeyInfo((char)0, ConsoleKey.F13, false, false, false);
        public static ConsoleKeyInfo F14 = new ConsoleKeyInfo((char)0, ConsoleKey.F14, false, false, false);
        public static ConsoleKeyInfo F15 = new ConsoleKeyInfo((char)0, ConsoleKey.F15, false, false, false);
        public static ConsoleKeyInfo F16 = new ConsoleKeyInfo((char)0, ConsoleKey.F16, false, false, false);
        public static ConsoleKeyInfo F17 = new ConsoleKeyInfo((char)0, ConsoleKey.F17, false, false, false);
        public static ConsoleKeyInfo F18 = new ConsoleKeyInfo((char)0, ConsoleKey.F18, false, false, false);
        public static ConsoleKeyInfo F19 = new ConsoleKeyInfo((char)0, ConsoleKey.F19, false, false, false);
        public static ConsoleKeyInfo F20 = new ConsoleKeyInfo((char)0, ConsoleKey.F20, false, false, false);
        public static ConsoleKeyInfo F21 = new ConsoleKeyInfo((char)0, ConsoleKey.F21, false, false, false);
        public static ConsoleKeyInfo F22 = new ConsoleKeyInfo((char)0, ConsoleKey.F22, false, false, false);
        public static ConsoleKeyInfo F23 = new ConsoleKeyInfo((char)0, ConsoleKey.F23, false, false, false);
        public static ConsoleKeyInfo F24 = new ConsoleKeyInfo((char)0, ConsoleKey.F24, false, false, false);

        public static ConsoleKeyInfo AltF1 = new ConsoleKeyInfo((char)0, ConsoleKey.F1, false, true, false);
        public static ConsoleKeyInfo AltF2 = new ConsoleKeyInfo((char)0, ConsoleKey.F2, false, true, false);
        public static ConsoleKeyInfo AltF3 = new ConsoleKeyInfo((char)0, ConsoleKey.F3, false, true, false);
        public static ConsoleKeyInfo AltF4 = new ConsoleKeyInfo((char)0, ConsoleKey.F4, false, true, false);
        public static ConsoleKeyInfo AltF5 = new ConsoleKeyInfo((char)0, ConsoleKey.F5, false, true, false);
        public static ConsoleKeyInfo AltF6 = new ConsoleKeyInfo((char)0, ConsoleKey.F6, false, true, false);
        public static ConsoleKeyInfo AltF7 = new ConsoleKeyInfo((char)0, ConsoleKey.F7, false, true, false);
        public static ConsoleKeyInfo AltF8 = new ConsoleKeyInfo((char)0, ConsoleKey.F8, false, true, false);
        public static ConsoleKeyInfo AltF9 = new ConsoleKeyInfo((char)0, ConsoleKey.F9, false, true, false);
        public static ConsoleKeyInfo AltFl0 = new ConsoleKeyInfo((char)0, ConsoleKey.F10, false, true, false);
        public static ConsoleKeyInfo AltF11 = new ConsoleKeyInfo((char)0, ConsoleKey.F11, false, true, false);
        public static ConsoleKeyInfo AltF12 = new ConsoleKeyInfo((char)0, ConsoleKey.F12, false, true, false);
        public static ConsoleKeyInfo AltF13 = new ConsoleKeyInfo((char)0, ConsoleKey.F13, false, true, false);
        public static ConsoleKeyInfo AltF14 = new ConsoleKeyInfo((char)0, ConsoleKey.F14, false, true, false);
        public static ConsoleKeyInfo AltF15 = new ConsoleKeyInfo((char)0, ConsoleKey.F15, false, true, false);
        public static ConsoleKeyInfo AltF16 = new ConsoleKeyInfo((char)0, ConsoleKey.F16, false, true, false);
        public static ConsoleKeyInfo AltF17 = new ConsoleKeyInfo((char)0, ConsoleKey.F17, false, true, false);
        public static ConsoleKeyInfo AltF18 = new ConsoleKeyInfo((char)0, ConsoleKey.F18, false, true, false);
        public static ConsoleKeyInfo AltF19 = new ConsoleKeyInfo((char)0, ConsoleKey.F19, false, true, false);
        public static ConsoleKeyInfo AltF20 = new ConsoleKeyInfo((char)0, ConsoleKey.F20, false, true, false);
        public static ConsoleKeyInfo AltF21 = new ConsoleKeyInfo((char)0, ConsoleKey.F21, false, true, false);
        public static ConsoleKeyInfo AltF22 = new ConsoleKeyInfo((char)0, ConsoleKey.F22, false, true, false);
        public static ConsoleKeyInfo AltF23 = new ConsoleKeyInfo((char)0, ConsoleKey.F23, false, true, false);
        public static ConsoleKeyInfo AltF24 = new ConsoleKeyInfo((char)0, ConsoleKey.F24, false, true, false);

        public static ConsoleKeyInfo ShiftF3 = new ConsoleKeyInfo((char)0, ConsoleKey.F3, true, false, false);
        public static ConsoleKeyInfo ShiftF8 = new ConsoleKeyInfo((char)0, ConsoleKey.F8, true, false, false);

        // Keys to ignore 
#if !CORECLR
        public static ConsoleKeyInfo VolumeUp   = new ConsoleKeyInfo((char)0, ConsoleKey.VolumeUp, false, false, false);
        public static ConsoleKeyInfo VolumeDown = new ConsoleKeyInfo((char)0, ConsoleKey.VolumeDown, false, false, false);
        public static ConsoleKeyInfo VolumeMute = new ConsoleKeyInfo((char)0, ConsoleKey.VolumeMute, false, false, false);
#endif
    }
}
