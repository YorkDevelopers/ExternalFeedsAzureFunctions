#load "category.csx"
#load "photo.csx"

public class Group
{
    public string name { get; set; }

    public Category category { get; set; }
    public Photo photo { get; set; }
}

