﻿using CoderDbc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoderDbc.Core
{
    public class CSharpSignalPrinter
    {
        public string PrintPackFunctionBody(SignalBitsDesc sig)
        {
            sb.Clear();
            return sb.ToString();
        }

        public string PrintUnpackFunctionBody(SignalBitsDesc sig)
        {
            sb.Clear();
            UInt16 startb = (UInt16)((sig.Order == BitLayout.Intel) ? (sig.StartBit + (sig.LengthBit - 1)) : (sig.StartBit));

            if (startb > 63)
                startb = 63;

            Int32 bn = startb / 8;
            Int32 bbc = (startb % 8) + 1;
            Int32 l = sig.LengthBit;
            sb.Clear();

            if (bbc > l)
            {
                sb.Append($"((_d[{bn}] >> {bbc - l}) & ({__lm[l]}))");
                sig.SigToByte[bn] = $"((_m.{sig.FieldName} & ({__lm[l]})) << {bbc - l})";
            }
            else if (bbc == l)
            {
                // no rolling bits
                sb.Append($"(_d[{bn}] & ({__lm[l]}))");
                sig.SigToByte[bn] = $"(_m.{sig.FieldName} & ({__lm[l]}))";
            }
            else
            {
                var type64_cov = String.Empty;
                l -= bbc;

                if (l > 31)
                {
                    type64_cov = "";
                }

                sb.Append($"({type64_cov}(_d[{bn}] & ({__lm[bbc]})) << {l})");
                sig.SigToByte[bn] = $"((_m.{sig.FieldName} >> {l}) & ({__lm[bbc]}))";

                while ((l - 8) >= 0)
                {
                    type64_cov = String.Empty;
                    l -= 8;
                    bn = ShiftByte(sig, bn);
                    sb.Append(" | ");

                    if (l == 0)
                    {
                        // last byte is aligned
                        sb.Append($"(_d[{bn}] & ({__lm[8]}))");
                        sig.SigToByte[bn] = $"(_m.{sig.FieldName} & ({__lm[8]}))";
                    }
                    else
                    {
                        if (l > 31)
                        {
                            type64_cov = "(UInt64)";
                        }

                        sb.Append($"({type64_cov}(_d[{bn}] & ({__lm[8]})) << {l})");
                        sig.SigToByte[bn] = $"((_m.{sig.FieldName} >> {l}) & ({__lm[8]}))";
                    }
                }

                if (l > 0)
                {
                    bn = ShiftByte(sig, bn);
                    sb.Append($" | ((_d[{ bn}] >> {8 - l}) & ({ __lm[l]}))");
                    sig.SigToByte[bn] = $"((_m.{sig.FieldName} & ({ __lm[l]})) << {8 - l})";
                }
            }

            return sb.ToString();
        }

        public string PrintSignalType(SignalBitsDesc sig)
        {
            sb.Clear();
            sb.Append(($"  public {__typeprint[(int)sig.SigType]} {sig.FieldName};").PadRight(35, ' '));
            string comment = $" // ";
            comment += (sig.Signed) ? " [-]" : "    ";
            comment += $" Bits={sig.LengthBit:D2}. ";
            comment += $" [ ";
            comment += sig.MinValue.ToViewable().PadRight(6, ' ');
            comment += ", ";
            comment += sig.MaxValue.ToViewable().PadRight(6, ' ');
            comment += " ] ";

            if (sig.FieldUnit != null)
            {
                comment += ($" Unit:'{sig.FieldUnit}'").PadRight(13, ' ');
            }

            if (sig.Offset != 0)
            {
                comment += ($" Offset= {sig.Offset}").PadRight(18, ' ');
            }

            if (sig.Factor != 1)
            {
                comment += ($" Factor= {sig.Factor}").PadRight(15, ' ');
            }

            return sb.ToString() + comment;
        }

        public string PrintSignalPackExpression(SignalBitsDesc sig)
        {
            sb.Clear();

            if (sig.Offset == 0 && sig.Factor == 1)
            {
                return null;
            }

            sb.Append($"// signal: @{sig.FieldName}" + Environment.NewLine);
            sb.AppendLine($"public const double {sig.FieldName}_CovFactor = {sig.Factor};");
            sb.Append($"public static {__typeprint[(int)sig.SigType]} {sig.FieldName}_CovS(double x) => ");
            //sb.Append($"{sig.FieldName}_CovS(x) ");
            var str = String.Empty;

            if (sig.Factor != 1)
                str += $"(x / {sig.Factor})";
            else
                str += "x";

            if (sig.Offset != 0)
            {
                if (sig.Offset < 0)
                {
                    str += " + " + Math.Abs(sig.RawOffset);
                }
                else
                {
                    str += " - " + Math.Abs(sig.RawOffset);
                }
            }
            else
            {
            }

            sb.Append($"({__typeprint[(int)sig.SigType]})({str});");
            return sb.ToString();
        }

        private int ShiftByte(SignalBitsDesc sg, int bn)
        {
            return (sg.Order == BitLayout.Intel) ? (bn - 1) : (bn + 1);
        }

        public string PrintType(SigTypeEnum en) { return __typeprint[(int)en]; }

        StringBuilder sb = new StringBuilder(256);
        static string[] __lm = { "0", "0x01U", "0x03U", "0x07U", "0x0FU", "0x1FU", "0x3FU", "0x7FU", "0xFFU" };
        static string[] __rm = { "0x80U", "0xC0U", "0xE0U", "0xF0U", "0xF8U", "0xFCU", "0xFEU", "0xFFU" };
        static string[] __op = { " << ", " >> " };
        static string[] __typeprint =
        {
            "char",
            "Int16",
            "Int32",
            "Int64",
            "byte",
            "UInt16",
            "UInt32",
            "UInt64"
        };
    }
}
