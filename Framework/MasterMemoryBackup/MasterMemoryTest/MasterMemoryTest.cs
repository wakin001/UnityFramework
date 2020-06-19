using MasterMemory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MasterData;

public class MasterMemoryTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CreateDatabase();
    }

    public void CreateDatabase()
    {
        // to create database, use DatabaseBuilder and Append method.
        var builder = new DatabaseBuilder();
        builder.Append(new Person[]
        {
            new Person { PersonId = 0, Age = 13, Gender = Gender.Male,   Name = "Dana Terry" },
            new Person { PersonId = 1, Age = 17, Gender = Gender.Male,   Name = "Kirk Obrien" },
            new Person { PersonId = 2, Age = 31, Gender = Gender.Male,   Name = "Wm Banks" },
            new Person { PersonId = 3, Age = 44, Gender = Gender.Male,   Name = "Karl Benson" },
            new Person { PersonId = 4, Age = 23, Gender = Gender.Male,   Name = "Jared Holland" },
            new Person { PersonId = 5, Age = 27, Gender = Gender.Female, Name = "Jeanne Phelps" },
            new Person { PersonId = 6, Age = 25, Gender = Gender.Female, Name = "Willie Rose" },
            new Person { PersonId = 7, Age = 11, Gender = Gender.Female, Name = "Shari Gutierrez" },
            new Person { PersonId = 8, Age = 63, Gender = Gender.Female, Name = "Lori Wilson" },
            new Person { PersonId = 9, Age = 34, Gender = Gender.Female, Name = "Lena Ramsey" },
        });

        // build database binary(you can also use `WriteToStream` for save to file).
        byte[] data = builder.Build();

        // -----------------------

        // for query phase, create MemoryDatabase.
        // (MemoryDatabase is recommended to store in singleton container(static field/DI)).
        var db = new MemoryDatabase(data);

        // .PersonTable.FindByPersonId is fully typed by code-generation.
        Person person = db.PersonTable.FindByPersonId(10);

        // Multiple key is also typed(***And * **), Return value is multiple if key is marked with `NonUnique`.
        RangeView<Person> result = db.PersonTable.FindByGenderAndAge((Gender.Female, 23));

        // Get nearest value(choose lower(default) or higher).
        RangeView<Person> age1 = db.PersonTable.FindClosestByAge(31);

        // Get range(min-max inclusive).
        RangeView<Person> age2 = db.PersonTable.FindRangeByAge(20, 29);
    }
}
