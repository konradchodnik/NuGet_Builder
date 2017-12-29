using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetBuild.Models
{
    public class DaneXml
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { id = value; }
        }
        private string authors;
        public string Authors
        {
            get { return authors; }
            set { authors = value; }
        }
        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        private string owners;
        public string Owners
        {
            get { return owners; }
            set { owners = value; }
        }
        private string releasenotes;
        public string ReleaseNotes
        {
            get { return releasenotes; }
            set { releasenotes = value; }
        }
        private string title;
        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        private string version;
        public string Version
        {
            get { return version; }
            set { version = value; }
        }


        public DaneXml(string id, string authors, string version, string description, string owners, string releaseNotes, string title)
        {
            Id = id;
            Authors = authors;
            Version = version;
            Description = description;
            Owners = owners;
            ReleaseNotes = releaseNotes;
            Title = title;
        }

        public DaneXml()
        {
        }
    }
}
