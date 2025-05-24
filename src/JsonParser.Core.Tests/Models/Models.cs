using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace JsonParser.Core.Tests.Models;

// Test Models
public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime? BirthDate { get; set; }
    public Address Address { get; set; }
    public List<string> Hobbies { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int ZipCode { get; set; }
}

public class PersonWithJsonNames
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("years_old")]
    public int YearsOld { get; set; }
}

public enum Status
{
    Active,
    Inactive,
    Pending
}

public class PersonWithEnum
{
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
}
