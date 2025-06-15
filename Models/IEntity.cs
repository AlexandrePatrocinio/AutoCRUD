namespace AutoCRUD.Models;

public interface IEntity<I> where I : struct
{
    I Id { get; set; }    
}
