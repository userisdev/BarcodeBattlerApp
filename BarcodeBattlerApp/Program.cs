using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BarcodeBattlerApp
{
    internal sealed class BarcodeBattlerItem
    {
        public BarcodeBattlerCardType CardType { get; }

        public BarcodeBattlerCharacterType CharacterType { get; }

        public int ST { get; }

        public int DF { get; }

        public int HP { get; }


        private BarcodeBattlerItem(BarcodeBattlerCardType cardType, BarcodeBattlerCharacterType charType, int hp, int st, int df)
        {
            CardType = cardType;
            CharacterType = charType;
            HP = hp;
            ST = st;
            DF = df;
        }

        private static (int, int, int) ReadFromFirst(string barcode, BarcodeBattlerCardType cardType)
        {
            var hp = int.Parse(barcode.Substring(0, 3)) * 100;
            var st = int.Parse(barcode.Substring(3, 2)) * 100;
            var df = int.Parse(barcode.Substring(5, 2)) * 100;
            if (hp >= 20000)
            {
                var d8 = int.Parse(barcode[7].ToString());
                switch (d8)
                {
                    case 0:
                        st += 10000;
                        break;
                    case 1:
                        df += 10000;
                        break;
                    case 2:
                        st += 10000;
                        df += 10000;
                        break;
                    case 3:
                    case 4:
                    default:
                        break;
                }
            }

            if (cardType != BarcodeBattlerCardType.Character)
            {
                hp = hp > 99900 ? 99900 : hp;
                st = st > 9900 ? 9900 : st;
                df = df > 9900 ? 9900 : df;
            }

            return (hp, st, df);
        }

        private static (int, int, int) ReadFromLast(string barcode, BarcodeBattlerCardType cardType)
        {
            var d12 = int.Parse(barcode[11].ToString());
            var d11 = int.Parse(barcode[10].ToString());
            var d10 = int.Parse(barcode[9].ToString());
            var d9 = int.Parse(barcode[8].ToString());

            if (cardType == BarcodeBattlerCardType.Character)
            {
                var hp = (d12 / 2) * 10000 + d11 * 1000 + d10 * 100;
                var tmp0 = d11 + 7;
                var tmp1 = d10 + 5;
                var st = (tmp0 > 11 ? tmp0 - 10 : tmp0) * 1000 + (tmp1 > 10 ? tmp1 - 10 : tmp1) * 100;
                var tmp2 = d10 + 7;
                var tmp3 = d9 + 7;
                var df = (tmp2 > 10 ? tmp2 - 10 : tmp2) * 1000 + (tmp3 > 10 ? tmp3 - 10 : tmp3) * 100;
                return (hp, st, df);
            }
            else
            {
                var hp = (d12 / 8) * 10000 + d11 * 1000 + d10 * 100;
                var tmp0 = d10 + 5;
                var st = GetST(d11) * 1000 + (tmp0 > 10 ? tmp0 - 10 : tmp0) * 100;
                var tmp1 = d9 + 7;
                var df = GetDF(d10) * 1000 + (tmp1 > 10 ? tmp1 - 10 : tmp1) * 100;
                return (hp, st, df);
            }
        }

        private static int GetDF(int value)
        {
            switch (value)
            {
                case 3:
                case 4:
                case 5:
                case 6:
                    return 0;

                case 7:
                case 8:
                case 9:
                case 0:
                    return 1;


                case 1:
                case 2:
                    return 2;

                default:
                    return 0;
            }
        }

        private static int GetST(int value)
        {
            switch (value)
            {
                case 5:
                case 6:
                case 7:
                case 8:
                    return 1;

                case 9:
                case 0:
                case 1:
                case 2:
                    return 2;


                case 3:
                case 4:
                    return 2;

                default:
                    return 0;
            }
        }

        private static BarcodeBattlerCardType GetCardType(string barcode)
        {
            switch (int.Parse(barcode[12].ToString()))
            {
                case 5:
                case 6:
                    return BarcodeBattlerCardType.ItemST;

                case 7:
                case 8:
                    return BarcodeBattlerCardType.ItemDF;

                case 9:
                    return BarcodeBattlerCardType.ItemHP;

                default:
                    return BarcodeBattlerCardType.Character;
            }
        }
        private static bool IsFirst(string barcode)
        {
            var fd3 = int.Parse(barcode[2].ToString());
            var ld4 = int.Parse(barcode[9].ToString());
            return fd3 == 9 && ld4 == 5;
        }


        public static BarcodeBattlerItem Load(string barcode)
        {
            var isFirst = IsFirst(barcode);
            var cardType = GetCardType(barcode);
            var charType = GetCharacterType(barcode, isFirst);
            var statusF = ReadFromFirst(barcode, cardType);
            var statusL = ReadFromLast(barcode, cardType);

            return isFirst
                ? new BarcodeBattlerItem(cardType, charType, statusF.Item1, statusF.Item2, statusF.Item3)
                : new BarcodeBattlerItem(cardType, charType, statusL.Item1, statusL.Item2, statusL.Item3);
        }

        private static BarcodeBattlerCharacterType GetCharacterType(string barcode, bool isFirst)
        {
            var c = isFirst ? barcode[7] : barcode[12];
            var value = int.Parse(c.ToString());
            switch (value)
            {
                case 0:
                    return BarcodeBattlerCharacterType.Machine;

                case 1:
                    return BarcodeBattlerCharacterType.Animal;

                case 2:
                    return BarcodeBattlerCharacterType.Fish;

                case 3:
                    return BarcodeBattlerCharacterType.Bird;

                case 4:
                    return BarcodeBattlerCharacterType.Human;

                default:
                    return BarcodeBattlerCharacterType.Unknown;
            }

        }
    }



    internal enum BarcodeBattlerCharacterType
    {
        Machine,
        Animal,
        Fish,
        Bird,
        Human,
        Unknown,
    }

    internal enum BarcodeBattlerCardType
    {
        Character,
        ItemST,
        ItemDF,
        ItemHP,
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var barcode = args.FirstOrDefault() ?? string.Empty;
            var regex = new Regex(@"^[0-9]{13}$");
            if (!regex.IsMatch(barcode))
            {
                Console.WriteLine("invalid code.");
                Environment.Exit(1);
            }

            var item = BarcodeBattlerItem.Load(barcode);
            Console.WriteLine(item.CardType);
            Console.WriteLine(item.CharacterType);
            Console.WriteLine(item.HP);
            Console.WriteLine(item.ST);
            Console.WriteLine(item.DF);
            Environment.Exit(0);
        }
    }
}
