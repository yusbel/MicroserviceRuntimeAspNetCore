using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class ArraySample
    {
        public void Sample()
        {

            int[,,] arr = new int[,,]
            {
                {
                    { 0, 0, 1, 2, 5 }, {4, 5, 6, 7, 5 }, {8, 9, 10, 7, 2 }
                },
                {
                    { 4, 6, 7, 9, 7 }, {5, 7, 3, 8, 9 }, {1, 2, 4, 6, 10 }
                }
            };

            Console.WriteLine($"Length {arr.Length}");
            Console.WriteLine($"Get length {arr.GetLength(1)}");

            int[][] jag = new int[][]
            {
                new int[]{ 3, 6, 1 },
                new int[]{ 7, 3, 9, 4},
                new int[]{ 1, 2, 4, 5, 8, 3},
                new int[]{ 1, 2, 4, 5, 8, 3}
            };

            for (int i = 0; i < jag.Length; i++)
                for (int j = 0; j < jag[i].Length; j++)
                {
                    Console.WriteLine($"Jag element {jag[i][j]}");
                }

            Console.WriteLine($"Jag length: {jag.Length}");
            Console.WriteLine($"Jag get length: {jag.GetLength(0)}");


            for (var i = 0; i < arr.GetLength(0); i++)
            {
                if (arr == null)
                    continue;

                for (var j = 0; j < arr.GetLength(1); j++)
                {
                    for (var k = 0; k < arr.GetLength(2); k++)
                    {
                        Console.WriteLine(arr[i, j, k]);
                    }
                }
            }



            var point = new Point();

            Console.WriteLine(point.y);

        }
    }
}
