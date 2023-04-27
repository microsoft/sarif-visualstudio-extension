using System;

// namespace MoreTests {
namespace MoreTests.Test1
{
    class Test
    {
        private string Name;

        string GetName()
        {
            return Name;
        }

        void Add2(int a, int b)
        /*
         * This method adds 2 integers.
         */
        {
            Console.WriteLine("return a + b");
            return a + b;
        }

        void Add(int a, int b /*, int c? */)
        {
            Console.WriteLine("This method simply contains \"return a + b\"");
            // return a + b + c;
            return a + b;
        }

        void Add3(int a, int b, int c)
        {
            Console.WriteLine("return a + b + c");
            return a + b + c;
        }
    }
}

namespace MoreTests./*Test1*/Test2
{
    class Test
    {
        private string Name;

        string GetName()
        {
            return Name;
        }
    }
}

/*
namespace MoreTests.Test3
{
    class Test
    {
        private string Name3;

        string GetName()
        {
            return Name3;
        }
    }
}
*/

//namespace MoreTests.Test4
//{
//    class Test
//    {
//        private string Name4;
//
//        string GetName()
//        {
//            return Name4;
//        }
//    }
//}

namespace MoreTests.Test5
{
    class Test
    {
        private string Name;

        string // Name is a string.
        GetName()
        /*
         * GetName returns the name of the this object.
         */
        {
            return Name;
        }

        // string GetName()
        // { 
        //     Name = "Test5";
        //     return Name; 
        // }        

        string SetName(string name)
        {
            Name = name;
        }
    }
}

namespace MoreTests//.Test1
{
    class Test
    {
        private string Name;

        string GetName()
        {
            return Name;
        }

        public void TestCommentsAndStrings()
        {
            var test = new Test5.Test();

            /*
             * Now let's mess with the code that tries to find comments and string literals.
             * Here's a single double-quote: "
             */

            var name = test.GetName();
            name = "This string does not start a comment: /*";
            test.SetName(name);

            /*
             * A final comment and double-quote: "
             */
        }

        public void TestEscapedStrings()
        {
            var test = new Test5.Test();
            var name = "This code will call \"test.SetName(name)\" after this string.";
            test.SetName(name);
        }

        public void TestDoubleEscape()
        {
            var test = new Test5.Test();
            var name = "Here is the first double-escape: \\";
            test.SetName(name);
            name = "Here is the second double-escape: \\";
        }
    }
}
