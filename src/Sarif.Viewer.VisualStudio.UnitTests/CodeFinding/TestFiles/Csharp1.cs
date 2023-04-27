using System;
using System.Collections.Generic;

// TODO: Rename to something else?
namespace Tests
{
    /// <summary>
    /// Just a class for testing line matching.
    /// </summary>
    class Test1
    {
        public string Name { get => "This is a test."; }
        public string SubExample { get => "var diff = Sub(5, 3);"; }

        /*
         * This method adds two numbers.
         */
        public int Add(int a, int b)
        {
            return a + b;
        }

        public void SumTests()
        {
            var nums = new List<int> { 1, 2, 3, 4, 5 };
            var sum = Sum(nums);
        }

        /*
         * Calculates the sum of all the given numbers.
         */
        public int Sum(List<int> numbers)
        {
            var sum = 0;
            foreach (var number in numbers)
            {
                // Calculate the sum.
                sum = Add(sum, number);
            }
            return sum;
        }

        public int Sub(int a, int b)
        {
            return a - b;
        }

        public void SubTests()
        {
            var diff = Sub(5, 3);
            //diff = Sub(3, 5);
            diff = Sub(1, 1);
        }
    }

    class Test2
    {
        private string Name = new string("Test2");

        public Test2(string name)
        {
            Name = name;
        }
    }

    // Shouldn't collide with "Test2".
    class Test22
    {
        public Test22(string name)
        {
            Name = name;
        }
    }

    class Utils
    {
        // Make sure we ignore < >.
        public static List<string> FlattenList(List<List<string>> doubleList)
        {
            var flatList = new List<string>();
            foreach (var list in doubleList)
            {
                flatList.AddRange(list);
            }
            return flatList;
        }

        // Make sure we ignore [].
        public static string[] ReverseArray(string[] array)
        {
            var newArray = new string[](array.Length);
            for (var i = array.Length - 1, j = 0; i >= 0; i--, j++)
            {
                newArray[j] = array[i];
            }
            return newArray;
        }

        public static IEnumerable<T> Randomize(List<T> list)
        {
            while (list.Count > 0)
            {
                var rand = new Random(DateTime.Now.Ticks);
                var next = rand.Next(0, list.Count);
                yield return list[next];
                list.RemoveAt(next);
            }
        }
    }
}
