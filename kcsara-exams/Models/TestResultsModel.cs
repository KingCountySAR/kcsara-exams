using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Kcsara.Exams.Models
{
  public class TestResultsModel
  {
    [HiddenInput]
    public Guid Id { get; set; }

    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Display(Name = "Email")]
    public string Email { get; set; }

    [HiddenInput]
    public string MemberId { get; set; }

    public string Title { get; set; }
    public string QuizId { get; set; }
    public DateTimeOffset Completed { get; set; }
    public TimeSpan Duration { get; set; }

    public bool Passed { get; set; }
    public int Score { get; set; }
    public int Possible { get; set; }
    public float Percentage { get; set; }

    public IEnumerable<string> Incorrect { get; set; } = new string[0];
  }
}
