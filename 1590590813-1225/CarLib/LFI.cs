using System;

namespace VehicleDynamics
{
	///<summary>
	///Class for Linear Function Interpolation
	///</summary>
	public class LFI  
	{
		private double[] data = new double[101];
		private double   slope = 1.0;
		private double   intercept = 0.0;

		public double Slope 
		{ 
			get { return slope; } 
			set { slope = value; } 
		}
		public double Intercept 
		{ 
			get { return intercept; } 
			set { intercept = value; } 
		}


	///<summary>
	///Method to place curve data into the class
	///</summary>
	public bool SetDataPoint(double index_value, float data_point)
{
	bool result = false;
	int index = (int)(index_value / slope - intercept);

	if ( index >= 0 && index <= 100 ) 
{
	data[index] = data_point;
	result = true;
}

			return result;

		}

		///<summary>
		///Method to interpolate linearly to get a value from a data curve.
		///</summary>
		public double Interpolate( double index_value )
		{
			double delta;
			double result = 0.0;

			try 
			{
				double scaled_value = index_value / slope - intercept;
				int index = (int)scaled_value;
				delta = data[index+1] - data[index];
				result = data[index] + delta * (scaled_value - index);
			}
			catch ( Exception e )
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}

			return result;

		}

	};
}
