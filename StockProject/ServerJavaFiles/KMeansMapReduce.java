package solution;

import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import org.apache.hadoop.conf.Configuration;
import org.apache.hadoop.fs.FileSystem;
import org.apache.hadoop.fs.Path;
import org.apache.hadoop.io.SequenceFile;
import org.apache.hadoop.mapreduce.Mapper;
import org.apache.hadoop.mapreduce.Reducer;

public class KMeansMapReduce {
	public static enum Counter {
		CONVERGED
	}
	
	public static class Map extends Mapper<Cluster, Vector, Cluster, Vector> {
		private static List<Cluster> _currKMeansClusters = new ArrayList<Cluster>();
		private static List<Cluster> _currCanopyClusters = new ArrayList<Cluster>();
		
		HashMap<Cluster, ArrayList<Vector>> _canopy = new HashMap<Cluster, ArrayList<Vector>>();
		
		@SuppressWarnings("deprecation")
		@Override
		protected void setup(Context context) throws IOException, InterruptedException {
			super.setup(context);
			_currKMeansClusters.clear();
			_currCanopyClusters.clear();
			
			Configuration conf = context.getConfiguration();
			Path centroids = new Path(conf.get("centers.path"));
			FileSystem fs = FileSystem.get(conf);
			SequenceFile.Reader reader = new SequenceFile.Reader(fs, centroids,	conf);
			
			Cluster canopyCenter = new Cluster();
			Cluster kMeansCenter = new Cluster();
	
			while (reader.next(canopyCenter, kMeansCenter)) {
				_currKMeansClusters.add(new Cluster(kMeansCenter));
				_currCanopyClusters.add(new Cluster(canopyCenter));
			}
			
			reader.close();
		}
	
		@Override
		protected void map(Cluster key, Vector value, Context context) throws IOException, InterruptedException {
			Cluster closestKmeansCenter = null;
			double closestDistance = Double.MAX_VALUE;
			
			for (int i = 0; i < _currKMeansClusters.size(); i++) {
				Cluster currCanopyCluster = new Cluster(_currCanopyClusters.get(i));
				Cluster kMeansCluster = new Cluster(_currKMeansClusters.get(i));
				
				if (key.compareTo(currCanopyCluster) == 0) {
					// get the distance between the vector and the current cluster
					double clusterDistance = Utils.getDistance(kMeansCluster, value);
					
					// if there is no nearest (i.e. first loop)
					if (closestKmeansCenter == null) {
						// replace the current center to be the nearest 
						closestKmeansCenter = kMeansCluster;
						closestDistance = clusterDistance;
					} else {
						// if the current center is closer then the current neKMeansMapReducearest center
						if (clusterDistance < closestDistance) {
							// replace the current center to be the nearest
							closestKmeansCenter = kMeansCluster;
							closestDistance = clusterDistance;
						}
					}
				}
			}
			
			context.write(closestKmeansCenter, value);
		}
	}
	
	public static class Reduce extends Reducer<Cluster, Vector, Cluster, Vector> {
		HashMap<Cluster, ArrayList<Vector>> _clusters = new HashMap<Cluster, ArrayList<Vector>>();
		public HashMap<Cluster,Cluster> _kMeansToCanopyMap;
		
		@SuppressWarnings("deprecation")
		@Override
		protected void setup(Context context) throws IOException, InterruptedException {
			super.setup(context);
			_kMeansToCanopyMap = new HashMap<Cluster,Cluster>();
			
			Cluster canopyCluster = new Cluster();
			Cluster kMeansCluster = new Cluster();
			
			Configuration conf = context.getConfiguration();
			Path centroids = new Path(conf.get("centers.path"));
			FileSystem fs = FileSystem.get(conf);
			SequenceFile.Reader reader = new SequenceFile.Reader(fs, centroids,	conf);
	
			while (reader.next(canopyCluster, kMeansCluster)) {
				_kMeansToCanopyMap.put(new Cluster(kMeansCluster), new Cluster(canopyCluster));
			}
			
			reader.close();
		}
		
		@Override
		protected void reduce(Cluster key, Iterable<Vector> values, Context context) throws IOException, InterruptedException {
			Cluster centerClone = new Cluster(key);
			
			if (!_clusters.containsKey(key))
				_clusters.put(centerClone, new ArrayList<Vector>());
	
			for (Vector vec : values)
				_clusters.get(centerClone).add(new Vector(vec));
		}
	
		@SuppressWarnings("deprecation")
		@Override
		protected void cleanup(Context context) throws IOException,	InterruptedException {
			super.cleanup(context);
			List<Cluster> newKMeansClusters = new ArrayList<Cluster>();
			List<Cluster> newCanopyClusters = new ArrayList<Cluster>();
			
			for (Cluster kMeansCluster : _clusters.keySet()) {			
				Cluster canopyCluster = _kMeansToCanopyMap.get(kMeansCluster);
		
				// Set a new Cluster center
				Vector center = new Vector();
				center.setElements(new double[kMeansCluster.getCenterVector().getElements().length]);
				List<Vector> vectors = new ArrayList<Vector>();
				
				for (Vector currentVector : _clusters.get(kMeansCluster)) {
					vectors.add(new Vector(currentVector));
	
					// Sums the vectors to a new vector in order to find the one that is the closest to all others, it will be our new cluster center.
					for (int i = 0; i < currentVector.getElements().length; i++)
						center.getElements()[i] += currentVector.getElements()[i];
				}
	
				// Divides the vector's elements in order to find its real location (it will be a fictive vector)
				for (int i = 0; i < center.getElements().length; i++)
					center.getElements()[i] = center.getElements()[i] / vectors.size();
	
				Cluster newKMeansCluster = new Cluster(center);
				canopyCluster.setIsCovered(newKMeansCluster.isConvergedWithOtherCluster(kMeansCluster));
				newKMeansClusters.add(newKMeansCluster);
				newCanopyClusters.add(canopyCluster);
				
				// Adding the vectors to the new cluster center
				for (Vector vector : vectors) {
					context.write(newKMeansCluster, vector);
				}
			}
	
			Configuration conf = context.getConfiguration();
			Path outPath = new Path(conf.get("centers.path"));
			FileSystem fs = FileSystem.get(conf);
			
			if (fs.exists(outPath)) 
				fs.delete(outPath, true);
	
			SequenceFile.Writer writer = SequenceFile.createWriter(fs, context.getConfiguration(), outPath, Cluster.class, Cluster.class);
			context.getCounter(Counter.CONVERGED).setValue(0);
			
			for (int i = 0; i < newKMeansClusters.size(); i++) {
				writer.append(newCanopyClusters.get(i), newKMeansClusters.get(i));
				
				if (newCanopyClusters.get(i).getIsCovered()) 
					context.getCounter(Counter.CONVERGED).increment(1);
			}

			writer.close();
		}
	}
}
