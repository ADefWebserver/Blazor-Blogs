﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace BlazorBlogs.Data.Models;

public partial class NewslettersCampain
{
    public int Id { get; set; }

    public string NewsletterCampainName { get; set; }

    public int NewsletterId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public virtual Newsletters Newsletter { get; set; }

    public virtual ICollection<NewslettersLogs> NewslettersLogs { get; set; } = new List<NewslettersLogs>();
}