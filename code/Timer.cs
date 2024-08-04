using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sandbox
{
    public class Timer
    {
        private DateTime startTime;
    	private TimeSpan elapsedTime;

		public Timer()
    	{
        	StartTimer();
    	}

	    public void StartTimer()
    	{
        	startTime = DateTime.Now;
    	}

		public TimeSpan GetElapsedTime()
    	{
        	elapsedTime = DateTime.Now - startTime;
        	return elapsedTime;
    	}

     	public void OnUpdate()
    	{
        	// Update the timer each tick
        	Log.Info($"Elapsed Time: {GetElapsedTime()}");
    	}
	
	}
}		
