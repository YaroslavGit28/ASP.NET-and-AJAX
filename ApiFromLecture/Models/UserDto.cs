using Microsoft.AspNetCore.Mvc.ModelBinding; namespace ApiFromLecture.Models; public class UserDto{public string Name{get;set;}="";[BindNever]public bool IsAdmin{get;set;}}
