using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NetSsa.IO
{
    public class FileIO
    {

        /*
            Returns the path of the newly created directory under Path.GetTempPath()
        */
        public static DirectoryInfo GetTempDirectory()
        {
            string path = null;

            do
            {
                path = Path.Combine(new String[] { Path.GetTempPath(), Path.GetRandomFileName() });
            } while (File.Exists(path));

            return Directory.CreateDirectory(path);
        }

        private static String COLUMN_SEPARATOR = "	";
        public static void WriteToFile(String filePath, IEnumerable<ITuple> tuples)
        {
            using (StreamWriter streamWriter = File.CreateText(filePath))
            {
                foreach (ITuple tuple in tuples)
                {
                    var elements = Enumerable.Range(0, tuple.Length).Select(index => tuple[index]);
                    streamWriter.WriteLine(String.Join(COLUMN_SEPARATOR, elements));
                }
            }
        }

        public static IEnumerable<ITuple> ReadFile(String filePath)
        {
            return File.ReadAllLines(filePath).Select(line => CreateTuple(line.Split(COLUMN_SEPARATOR)));
        }

        private static ITuple CreateTuple(String[] elements)
        {
            switch (elements.Length)
            {
                case 1:
                    return System.ValueTuple.Create(elements[0]);
                case 2:
                    return System.ValueTuple.Create(elements[0], elements[1]);
                case 3:
                    return System.ValueTuple.Create(elements[0], elements[1], elements[2]);
                default:
                    // Complete missing cases :)
                    throw new ArgumentOutOfRangeException("Unhandled size.");
            }
        }
    }
}
