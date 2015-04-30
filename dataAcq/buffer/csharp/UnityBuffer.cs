//using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using FieldTrip.Buffer;

//A delegate to be used as an Event Handler for changes in the buffer that need to notify other objects
public  delegate void BufferChangeEventHandler(UnityBuffer buffer,EventArgs e);

//By design this class holds only the latest data package from the buffer and registers the number of lost packages (i.e. packages that haven't been gotten) since the last time a package was gotten.
//Since it notifies for any data update it is the responsibility of the object who gets notified and uses the data to create some kind of local buffer if memory is required and store old data.
//TO DO: Add Writing data (of all types) into the buffer
//public class UnityBuffer : MonoBehaviour {
public class UnityBuffer
{

	public string hostname = "localhost";
	public int port = 1972;
	public int nSamples;
	public int nChans;
	public float fSample;
	public bool newDataIn;
	public int dataPacketsLost;
	public int bufferEventsMaxCapacity = 100;
	
	private Header hdr;
	private BufferClient bufferClient;
	private int latestBufferSample;
	private int latestCapturedSample;
	private int timeout = 10000;
	//msecs
	private object data;
	//Holds the latest data of the fieldtrip buffer between the two last updates of the fieldtrip buffer
	private int lastNumberOfEvents;
	private int latestNumebrOfEventsInBuffer;
	private List<BufferEvent> bufferEvents;
	//Holds the last bufferEventsMaxCapacity events as they are added to the fieldtrip buffer
	
	//An event that notifies when new data have been captured in the buffer
	public event BufferChangeEventHandler NewDataCaptured;
	
	//An event that notifies when new events have been put in the buffer
	public event BufferChangeEventHandler NewEventsAdded;

	protected virtual void OnNewDataCaptured(EventArgs e)
	{
		if (NewDataCaptured != null) {
			NewDataCaptured(this, e);
		}
	}

	protected virtual void OnNewEventsAdded(EventArgs e)
	{
		if (NewEventsAdded != null) {
			NewEventsAdded(this, e);
		}
	}

	
	public UnityBuffer()
	{
	
		bufferClient = new BufferClient();
		bufferEvents = new List<BufferEvent>();
		//Debug.Log("Connecting to "+hostname+":"+port);
		if (bufferClient.Connect(hostname, port)) {
			hdr = bufferClient.GetHeader();
			latestBufferSample = hdr.NumSamples;
			nSamples = hdr.NumSamples;
			nChans = hdr.NumChans;
			fSample = hdr.FSample;
			initializeData();
			//  System.Windows.Forms.MessageBox.Show("hello");
			//Debug.Log("#channels....: "+hdr.nChans);
			//Debug.Log("#samples.....: "+hdr.nSamples);
			//Debug.Log("#events......: "+hdr.nEvents);
			//Debug.Log("Sampling Freq: "+hdr.fSample);
			//Debug.Log("data type....: "+hdr.dataType);
			
			for (int n = 0; n < nChans; n++) {
				if (hdr.Labels[n] != null) {
					//Debug.Log("Channel number " + n + ": " + hdr.labels[n]);
				}
			}
		} else //Debug.LogError("Connection to "+hostname+":"+port+" failed");
		
		latestCapturedSample = 0;
		newDataIn = false;
		dataPacketsLost = 0;
		lastNumberOfEvents = 0;
		latestNumebrOfEventsInBuffer = 0;
	}

	
	
	private void initializeData()
	{
		int dataType = hdr.DataType;
		switch (dataType) {
			case DataType.CHAR:
				data = new char[nSamples, nChans];
				break;
					
			case DataType.INT8:
				goto case DataType.UINT8;
			case DataType.UINT8:
				data = new byte[nSamples, nChans];
				break;
					
			case DataType.INT16:
				goto case DataType.UINT16;
			case DataType.UINT16:
				data = new short[nSamples, nChans];
				break;
					
			case DataType.INT32:
				goto case DataType.UINT32;
			case DataType.UINT32:
				data = new int[nSamples, nChans];
				break;
					
			case DataType.INT64:
				goto case DataType.UINT64;
			case DataType.UINT64:
				data = new long[nSamples, nChans];
				break;
					
			case DataType.FLOAT32:
				data = new float[nSamples, nChans];
				break;
					
			case DataType.FLOAT64:
				data = new double[nSamples, nChans];
				break;
					
			default:
				//Debug.LogError("Uknown data format received from Buffer");
				break;
		}
	}

	
	
	public void Update()
	{
		if (bufferClient.errorReturned != BufferClient.BUFFER_READ_ERROR) {
			SamplesEventsCount count = bufferClient.Wait(latestCapturedSample + 1, lastNumberOfEvents + 1, timeout); 
			latestNumebrOfEventsInBuffer = count.NumEvents;
			latestBufferSample = count.NumSamples;
			
			while (lastNumberOfEvents < latestNumebrOfEventsInBuffer) {
				bufferEvents.Add(bufferClient.GetEvents(lastNumberOfEvents, lastNumberOfEvents)[0]);
				lastNumberOfEvents += 1;
				if (bufferEvents.Count > bufferEventsMaxCapacity) {
					bufferEvents.RemoveAt(0);
				}
				OnNewEventsAdded(EventArgs.Empty);//This notifies anyone who's listening that there had been an extra event added in the buffer
			}
			
			if (latestBufferSample > latestCapturedSample) {
				nSamples = latestBufferSample - latestCapturedSample;
				data = bufferClient.GetFloatData(latestCapturedSample, latestBufferSample - 1); //TO DO: The getFloat needs to change according to the buffers type of data
				latestCapturedSample = latestBufferSample;
				OnNewDataCaptured(EventArgs.Empty); //That notifies anyone who's listening that data have been updated in the buffer
				if (newDataIn)
					dataPacketsLost += 1;
				else
					newDataIn = true;
			}
		}
	}

	
	public T[,] GetData<T>()
	{
		return (T[,])data;
	}
	/*    LOUK   */
	public BufferEvent GetEvent(int eventnumber)
	{
		return bufferEvents[eventnumber - 1];
	}
	/*    LOUK   */
	public BufferEvent GetLatestEvent()
	{
		return bufferEvents[lastNumberOfEvents - 1];
	}

	public int GetCurrentSampleNumber()
	{
		return latestBufferSample;
	}

	
	public void PutEvent<T>(string type, T val, int sample)
	{
		
		Type cls = typeof(T);
		string typeOfVal = cls.FullName;
		if (cls.IsArray) {
			Type elc = cls.GetElementType();
			if (!elc.IsPrimitive)
				return;
			
			if (typeOfVal == "System.String[]") {
				string[] temp = (string[])(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Byte[]") {
				byte[] temp = (byte[])(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Int16[]") {
				short[] temp = (short[])(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);				
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Int32[]") {
				int[] temp = (int[])(object)val;												
				BufferEvent ev = new BufferEvent(type, temp, sample);		
				bufferClient.PutEvent(ev);	
			}
			if (typeOfVal == "System.Int64[]") {
				long[] temp = (long[])(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Single[]") {
				float[] temp = (float[])(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);			
				bufferClient.PutEvent(ev);	
			}
			if (typeOfVal == "System.Double[]") {
				double[] temp = (double[])(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
		} else {
			if (typeOfVal == "System.String") {
				string temp = (string)(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Byte") {
				byte temp = (byte)(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Int16") {
				short temp = (short)(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);				
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Int32") {
				int temp = (int)(object)val;												
				BufferEvent ev = new BufferEvent(type, temp, sample);		
				bufferClient.PutEvent(ev);	
			}
			if (typeOfVal == "System.Int64") {
				long temp = (long)(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
			if (typeOfVal == "System.Single") {
				float temp = (float)(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);			
				bufferClient.PutEvent(ev);	
			}
			if (typeOfVal == "System.Double") {
				double temp = (double)(object)val;
				BufferEvent ev = new BufferEvent(type, temp, sample);
				bufferClient.PutEvent(ev);
			}
		}
	}

}

