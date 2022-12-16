using UnityEngine;
using NUnitLite;
using ProtoPromiseTests;
using System.Collections.Generic;

public class TestRunner : MonoBehaviour
{
    void Start()
    {
        // Command line args include the executable name and unity args, so we need to strip them to only include nunit args.
        string[] args = System.Environment.GetCommandLineArgs();
        int i = 0;
        while (i < args.Length)
        {
            if (args[i].StartsWith("--"))
            {
                break;
            }
            ++i;
        }
        List<string> nUnitArgs = new List<string>(args.Length - i);
        for (; i < args.Length; ++i)
        {
            nUnitArgs.Add(args[i]);
        }
        int quitCode = new AutoRun(typeof(TestHelper).Assembly).Execute(nUnitArgs.ToArray());
        Application.Quit(quitCode);
    }
}