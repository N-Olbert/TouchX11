using System;
using JetBrains.Annotations;
using TX11Business.BusinessObjects.Keyboard;
using TX11Business.Compatibility;
using TX11Shared;
using TX11Shared.Keyboard;

namespace TX11Business
{
    internal class Keyboard
    {
        private int minimumKeycode;
        private int numKeycodes;
        private byte keysymsPerKeycode = 3;
        private int[] keyboardMapping;
        private readonly byte keycodesPerModifier = 2;
        private readonly byte[] keymap = new byte[32];
        [NotNull]
        private readonly ModifierMapping modifierMapping;

        private const int DefaultBellPercent = 50;
        private int bellPercent = DefaultBellPercent;
        private const int DefaultBellPitch = 400;
        private int bellPitch = DefaultBellPitch;
        private const int DefaultBellDuration = 100;
        private int bellDuration = DefaultBellDuration;
        private short[] bellBuffer;

        private bool bellBufferFilled;
        //private AudioTrack _audioTrack = null;

        private const int SampleRate = 11025;
        private const int KeyClickPercent = 0;
        private const int BellPercent = 1;
        private const int BellPitch = 2;
        private const int BellDuration = 3;
        private const int Led = 4;
        private const int LedMode = 5;
        private const int Key = 6;
        private const int AutoRepeatMode = 7;

        /**
         * Constructor.
         */
        internal Keyboard()
        {
            var kpk = keysymsPerKeycode;
            var min = 255;
            var max = 0;
            var idx = 0;
            var map = new int[256 * kpk];

            var keyMapper = XConnector.GetInstanceOf<IXKeyCharMapper>();
            this.modifierMapping = new ModifierMapping();
            this.modifierMapping.SetShiftMapping(keyMapper.KeycodeShiftLeft, keyMapper.KeycodeShiftRight);
            this.modifierMapping.SetControlMapping(keyMapper.KeycodeCtrlLeft, keyMapper.KeycodeCtrlRight);
            this.modifierMapping.SetMod1Mapping(keyMapper.KeycodeAltLeft, keyMapper.KeycodeAltRight);

            var mapper = XConnector.GetInstanceOf<IXKeyCharMapper>();
            for (var i = 0; i < 256; i++)
            {
                int c1 = mapper.GetMappedChar(new XKeyEvent(i, false, false, false));
                int c2 = mapper.GetMappedChar(new XKeyEvent(i, true, false, false));
                int c3 = mapper.GetMappedChar(new XKeyEvent(i, false, true, false));

                map[idx++] = c1;
                map[idx++] = c2;
                map[idx++] = c3;

                if (c1 != 0 || c2 != 0 || c3 != 0)
                {
                    if (i < min)
                        min = i;
                    if (i > max)
                        max = i;
                }
            }

            if (max == 0)
                min = 0;

            if (max < keyMapper.DeleteKeyCode)
                max = keyMapper.DeleteKeyCode;

            minimumKeycode = min;
            numKeycodes = max - min + 1;
            if (numKeycodes > 248)
                numKeycodes = 248;

            keyboardMapping = new int[kpk * numKeycodes];
            Array.Copy(map, min * kpk, keyboardMapping, 0, keyboardMapping.Length);

            keyboardMapping[(keyMapper.DeleteKeyCode - min) * kpk] = 127;
            keyboardMapping[(keyMapper.KeycodeAltLeft - min) * kpk] = 0xff7e;
            keyboardMapping[(keyMapper.KeycodeAltRight - min) * kpk] = 0xff7e;
        }

        /**
         * Translate an Android keycode to an X keycode.
         *
         * @param keycode	The Android keycode.
         * @return	The corresponding X keycode.
         */
        internal int TranslateToXKeycode(int keycode)
        {
            if (minimumKeycode < 8)
                return keycode + 8 - minimumKeycode;
            else
                return keycode;
        }

        /**
         * Return the minimum keycode.
         *
         * @return	The minimum keycode.
         */
        internal int GetMinimumKeycode()
        {
            if (minimumKeycode < 8)
                return 8;
            else
                return minimumKeycode;
        }

        /**
         * Return the minimum keycode diff.
         *
         * @return	The minimum keycode.
         */
        private int GetMinimumKeycodeDiff()
        {
            if (minimumKeycode < 8)
                return 8 - minimumKeycode;
            else
                return 0;
        }

        /**
         * Return the maximum keycode.
         *
         * @return	The maximum keycode.
         */
        internal int GetMaximumKeycode()
        {
            return GetMinimumKeycode() + numKeycodes - 1;
        }

        /**
         * Return the keymap for keycodes 8-255.
         *
         * @return	The keymap for keycodes 8-255.
         */
        internal byte[] GetKeymap()
        {
            var keymap = new byte[31];

            Array.Copy(this.keymap, 1, keymap, 0, 31);

            return keymap;
        }

        internal int GetModifierMask(XKeyEvent e, bool pressed)
        {
            return this.modifierMapping.GetModifierMask(e, pressed);
        }

        /**
         * Update the keymap when a key is pressed or released.
         *
         * @param keycode	The keycode of the key.
         * @param pressed	True if pressed, false if released.
         */
        internal void UpdateKeymap(int keycode, bool pressed)
        {
            if (keycode < 0 || keycode > 255)
                return;

            var offset = keycode / 8;
            var mask = (byte) (1 << (keycode & 7));

            if (pressed)
                keymap[offset] |= mask;
            else
                keymap[offset] = (keymap[offset] & ~mask).AsByte();
        }

        /**
         * Process an X request relating to this keyboard.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal void ProcessRequest(XServer xServer, Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.QueryKeymap:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(2); // Reply length.
                            io.WriteBytes(keymap, 0, 32); // Keys.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.ChangeKeyboardMapping:
                    if (bytesRemaining < 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var keycodeCount = arg;
                        var keycode = (byte) io.ReadByte(); // First code.
                        var kspkc = (byte) io.ReadByte(); // Keysyms per code.

                        io.ReadSkip(2); // Unused.
                        bytesRemaining -= 4;

                        if (bytesRemaining != keycodeCount * kspkc * 4)
                        {
                            io.ReadSkip(bytesRemaining);
                            ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                        }
                        else
                        {
                            minimumKeycode = keycode;
                            numKeycodes = keycodeCount;
                            keysymsPerKeycode = kspkc;
                            keyboardMapping = new int[keycodeCount * kspkc];
                            for (var i = 0; i < keyboardMapping.Length; i++)
                                keyboardMapping[i] = io.ReadInt(); // Keysyms.

                            xServer.SendMappingNotify(1, keycode, keycodeCount);
                        }
                    }

                    break;
                case RequestCode.GetKeyboardMapping:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var keycode = io.ReadByte(); // First code.
                        var count = io.ReadByte(); // Count.
                        var length = count * keysymsPerKeycode;
                        var offset = (keycode - GetMinimumKeycode()) * keysymsPerKeycode;

                        io.ReadSkip(2); // Unused.

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, keysymsPerKeycode);
                            io.WriteInt(length); // Reply length.
                            io.WritePadBytes(24); // Unused.

                            for (var i = 0; i < length; i++)
                            {
                                var n = i + offset;

                                if (n < 0 || n >= keyboardMapping.Length)
                                    io.WriteInt(0); // No symbol.
                                else
                                    io.WriteInt(keyboardMapping[n]);
                            }
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.ChangeKeyboardControl:
                    if (bytesRemaining < 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var valueMask = io.ReadInt(); // Value mask.
                        var nbits = Util.Bitcount(valueMask);

                        bytesRemaining -= 4;
                        if (bytesRemaining != nbits * 4)
                        {
                            io.ReadSkip(bytesRemaining);
                            ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                        }
                        else
                        {
                            for (var i = 0; i < 23; i++)
                                if ((valueMask & (1 << i)) != 0)
                                    ProcessValue(io, i);
                        }
                    }

                    break;
                case RequestCode.GetKeyboardControl:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        lock (io)
                        {
                            Util.WriteReplyHeader(client, keysymsPerKeycode);
                            io.WriteInt(5); // Reply length.
                            io.WriteInt(0); // LED mask.
                            io.WriteByte((byte) 0); // Key click percent.
                            io.WriteByte((byte) bellPercent); // Bell volume.
                            io.WriteShort((short) bellPitch); // Bell pitch Hz.
                            io.WriteShort((short) bellDuration);
                            io.WritePadBytes(2); // Unused.
                            io.WritePadBytes(32); // Auto repeats. Ignored.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.SetModifierMapping:
                    if (bytesRemaining != 8 * arg)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        // Not supported. Always fails.
                        io.ReadSkip(bytesRemaining);
                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 2);
                            io.WriteInt(0); // Reply length.
                            io.WritePadBytes(24); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.GetModifierMapping:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var kpm = keycodesPerModifier;
                        byte[] map = null;

                        if (kpm > 0)
                        {
                            var diff = GetMinimumKeycodeDiff();

                            map = new byte[kpm * 8];
                            for (var i = 0; i < map.Length; i++)
                                if (modifierMapping[i] == 0)
                                    map[i] = 0;
                                else
                                    map[i] = (byte) (modifierMapping[i] + diff);
                        }

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, kpm);
                            io.WriteInt(kpm * 2); // Reply length.
                            io.WritePadBytes(24); // Unused.

                            if (map != null)
                                io.WriteBytes(map, 0, map.Length);
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.Bell:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        PlayBell((byte) arg);
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Play a beep.
         *
         * @param percent	Volume relative to base volume, [-100, 100]
         */
        private void PlayBell(int percent)
        {
            Console.WriteLine("Bell");
            //int volume;

            //if (percent < 0)
            //{
            //	volume = _bellPercent + _bellPercent * percent / 100;
            //	_bellBufferFilled = false;
            //}
            //else if (percent > 0)
            //{
            //	volume = _bellPercent - _bellPercent * percent / 100 + percent;
            //	_bellBufferFilled = false;
            //}
            //else
            //{
            //	volume = _bellPercent;
            //}

            //if (_bellBuffer == null)
            //{
            //	_bellBuffer = new short[SAMPLE_RATE * _bellDuration / 1000];
            //	_bellBufferFilled = false;

            //}

            //if (!_bellBufferFilled)
            //{
            //	double vol = 32767.0 * (double)volume / 100.0;
            //	double dt = _bellPitch * 2.0 * Math.PI / SAMPLE_RATE;

            //	for (int i = 0; i < _bellBuffer.Length; i++)
            //		_bellBuffer[i] = (short)(vol * Math.sin((double)i * dt));

            //	_bellBufferFilled = true;
            //}

            //if (_audioTrack != null)
            //{
            //	_audioTrack.stop();
            //	_audioTrack.release();
            //}

            //_audioTrack = new AudioTrack(AudioManager.STREAM_SYSTEM,
            //			SAMPLE_RATE, AudioFormat.CHANNEL_CONFIGURATION_MONO,
            //			AudioFormat.ENCODING_PCM_16BIT, 2 * _bellBuffer.Length,
            //			AudioTrack.MODE_STATIC);

            //_audioTrack.write(_bellBuffer, 0, _bellBuffer.Length);
            //_audioTrack.play();
        }

        /**
         * Process a single keyboard attribute value.
         *
         * @param io	The input/output stream.
         * @param maskBit	The mask bit of the attribute.

         */
        private void ProcessValue(InputOutput io, int maskBit)
        {
            switch (maskBit)
            {
                case KeyClickPercent:
                    io.ReadByte(); // Not implemented.
                    io.ReadSkip(3);
                    break;
                case BellPercent:
                    bellPercent = (byte) io.ReadByte();
                    if (bellPercent < 0)
                        bellPercent = DefaultBellPercent;
                    io.ReadSkip(3);
                    bellBufferFilled = false;
                    break;
                case BellPitch:
                    bellPitch = (short) io.ReadShort();
                    if (bellPitch < 0)
                        bellPitch = DefaultBellPitch;
                    io.ReadSkip(2);
                    bellBufferFilled = false;
                    break;
                case BellDuration:
                    bellDuration = (short) io.ReadShort();
                    if (bellDuration < 0)
                        bellDuration = DefaultBellDuration;
                    io.ReadSkip(2);
                    bellBuffer = null;
                    break;
                case Led:
                    io.ReadByte(); // Not implemented.
                    io.ReadSkip(3);
                    break;
                case LedMode:
                    io.ReadByte(); // Not implemented.
                    io.ReadSkip(3);
                    break;
                case Key:
                    io.ReadByte(); // Not implemented.
                    io.ReadSkip(3);
                    break;
                case AutoRepeatMode:
                    io.ReadByte(); // Not implemented.
                    io.ReadSkip(3);
                    break;
            }
        }
    }
}