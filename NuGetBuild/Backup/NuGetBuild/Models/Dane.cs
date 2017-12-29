using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetBuild.Models
{
    public class Dane
    {
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string path;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }


        public Dane(string name)
        {
            Name = name;
        }

        public Dane()
        {
        }
    }
}
