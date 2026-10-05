using ApiFromLecture.Validation; namespace ApiFromLecture.Models; public class EventRequest { [NotInFuture] public DateTime Date{get;set;} }
