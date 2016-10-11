using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellReport.Core
{
    internal static class ArrayHelper
    {
        public static int GetLength<T>(T[] value)
        {
            return value == null ? 0 : value.Length;
        }

        public static void SetNewLength<T>(ref T[] value, int newLength, T defaultValue)
        {
            if (newLength <= 0)
                throw new ArgumentException();

            int oldLength = GetLength(value);
            if (oldLength == newLength)
                return;
            var newArray = new T[newLength];
            if (oldLength == 0)
            {
                for (int i = 0; i < newArray.Length; i++)
                    newArray[i] = defaultValue;
            } 
            else if (oldLength > newLength)
            {
                Array.Copy(value, newArray, newLength);
            }
            else if (oldLength < newLength)
            {
                Array.Copy(value, newArray, oldLength);
                for (int i = oldLength; i < newArray.Length; i++)
                    newArray[i] = defaultValue;
            }
            value = newArray;
        }

        public static void SetNewLength<T>(ref T[][] value, int newLength1, int newLength2)
        {
            int oldLength1 = GetLength(value);
            if (oldLength1 == 0)
            {
                value = new T[newLength1][];
                for (int i = 0; i < value.Length; i++)
                    value[i] = new T[newLength2];
                return;
            }
            if (oldLength1 != newLength1)
            {
                SetNewLength(ref value, newLength1, null);
                for (int i = oldLength1; i < newLength1; i++)
                    value[i] = new T[newLength2];
            }
            var oldLength2 = GetLength(value[0]);
            if (oldLength2 != newLength2)
            {
                for (int i = 0; i < newLength1; i++)
                    SetNewLength(ref value[i], newLength2, default(T));
            }
        }
    }
}
