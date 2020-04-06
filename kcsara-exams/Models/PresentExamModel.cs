using System;
using System.ComponentModel.DataAnnotations;
using Kcsara.Exams.Data;
using Microsoft.AspNetCore.Mvc;

namespace Kcsara.Exams.Models
{
  public class PresentExamModel
  {
    [HiddenInput]
    public Guid Id { get; set; }

    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Display(Name = "Email")]
    public string Email { get; set; }
    
    [HiddenInput]
    public string MemberId { get; set; }
    public Quiz Quiz { get; set; }
    [HiddenInput]
    public DateTimeOffset Started { get; set; }
  }
}
