using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Framework;

public class AsyncTest : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        Debug.Log("Waiting 1 second...");
        //await Task.Delay(System.TimeSpan.FromSeconds(1));

        await System.TimeSpan.FromSeconds(1);

        Debug.Log("Done!");

    }


}
