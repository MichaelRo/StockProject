package solution;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Hashtable;
import java.util.Map.Entry;
import org.apache.hadoop.fs.FileSystem;
import org.apache.hadoop.fs.Path;
import org.apache.hadoop.io.LongWritable;
import org.apache.hadoop.io.SequenceFile;
import org.apache.hadoop.io.Text;
import org.apache.hadoop.mapreduce.Mapper;
import org.apache.hadoop.mapreduce.Reducer;

public class CanopyMapReduce {
	public static class Map extends Mapper<LongWritable, Text, Cluster, Vector> {
		private static ArrayList<Cluster> _canopyCenters = new ArrayList<Cluster>();
		private static int _canopyMapperCounter = 0;
		
		public void map(LongWritable key, Text textValue, Context context) throws IOException, InterruptedException {	
			String value = textValue.toString();
			String name = value.substring(0, value.indexOf(' '));
	        String[] stocks = value.substring(value.indexOf(' ') + 1).split(",");
			
			double[] stockElements = new double[stocks.length];
			int i = 0;
			
			for(String s: stocks)
	        	stockElements[i++] = Double.parseDouble(s);
			
			Vector stockVector = new Vector();
			
			stockVector.setElements(stockElements);
			stockVector.setName(name);
			
			double T1 = Double.parseDouble(context.getConfiguration().get("T1"));
			boolean isClusterFound = false;
			
			for (Cluster cCurrCenter : _canopyCenters) {
				if (Utils.getDistance(cCurrCenter, stockVector) < T1) {
					context.write(cCurrCenter, stockVector);
					isClusterFound = true;
					break;
				}
			}

			// If no center is close enough to be a cluster center
			if (!isClusterFound) {
				Cluster newCenter = new Cluster(stockVector, Map._canopyMapperCounter);
				_canopyCenters.add(newCenter);
				context.write(newCenter, stockVector);
			}
			
			increaseCanopyMapperCounter();
		}
		
		private static void increaseCanopyMapperCounter() {
			_canopyMapperCounter++;
		}
	}
	
	public static class Reduce extends Reducer<Cluster, Vector, Text, Text> {
		public static Hashtable<Cluster, ArrayList<Vector>> reducedCenters = new Hashtable<Cluster, ArrayList<Vector>>();
		
		public void reduce(Cluster key, Iterable<Vector> values, Context context) throws IOException, InterruptedException {
			double T1 = Double.parseDouble(context.getConfiguration().get("T1"));
			boolean isClusterFound = false;

			for (Entry<Cluster, ArrayList<Vector>> currCenterEntry : reducedCenters.entrySet()) {
				Cluster cCurrCenter = currCenterEntry.getKey();

				if (Utils.getDistance(cCurrCenter, key.getCenterVector()) < T1) {
					// Reduce centers by "merging the centers vectors" to same file according the center id
					writeVectorsToCenterFile(context , String.format("StocksProject/Data/canopy%d", cCurrCenter.getId()), cCurrCenter, values);
					
					isClusterFound = true;
					break;
				}
			}

			// If no center is close enough to be cluster with the current map iteration
			if (!isClusterFound)
				writeVectorsToCenterFile(context , String.format("StocksProject/Data/canopy%d", key.getId()), key, values);
		}
		
		private void writeVectorsToCenterFile(Context context, String fullPath, Cluster cCenterFile, Iterable<Vector> values) throws IOException {
			FileSystem fs = FileSystem.get(context.getConfiguration());
			@SuppressWarnings("deprecation")
			SequenceFile.Writer writer = SequenceFile.createWriter(fs, context.getConfiguration(), new Path(fullPath), Cluster.class, Vector.class);

			for (Vector vCurrVector : values)
				writer.append(cCenterFile, vCurrVector);
			
			writer.close();
			fs.close();
		}
	}
}
