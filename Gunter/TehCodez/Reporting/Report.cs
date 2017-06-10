﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    public interface IReport : IResolvable
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        string Title { get; set; }

        [JsonRequired]
        List<ISection> Sections { get; set; }
    }

    public class Report : IReport
    {
        private string _title;

        [JsonIgnore]
        public IConstantResolver Constants { get; set; } = ConstantResolver.Empty;

        public int Id { get; set; }

        public string Title
        {
            get => Constants.Resolve(_title);
            set => _title = value;
        }

        public List<ISection> Sections { get; set; } = new List<ISection>();
    }
}
