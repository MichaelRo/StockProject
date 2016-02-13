package solution;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map.Entry;

import org.apache.hadoop.conf.Configuration;
import org.apache.hadoop.fs.FSDataOutputStream;
import org.apache.hadoop.fs.FileStatus;
import org.apache.hadoop.fs.FileSystem;
import org.apache.hadoop.fs.Path;
import org.apache.hadoop.io.SequenceFile;
import org.apache.hadoop.mapreduce.Job;
import org.apache.hadoop.mapreduce.lib.input.FileInputFormat;
import org.apache.hadoop.mapreduce.lib.input.SequenceFileInputFormat;
import org.apache.hadoop.mapreduce.lib.input.TextInputFormat;
import org.apache.hadoop.mapreduce.lib.output.SequenceFileOutputFormat;
import org.apache.hadoop.mapreduce.lib.partition.HashPartitioner;

public class Main {
	private static Collection<Integer> _randomIndexesList = new ArrayList<Integer>();
	
	@SuppressWarnings("deprecation")
	public static void main(String[] args) throws Exception {
		String inputDir = args[0];
		String outputDir = args[1];
		String T1 = args[2].equals("null") ? "0.5" : args[2];
		Integer k = Integer.decode(args[3].equals("null") ? "5" : args[3]);
		
		executeCanopy(inputDir, "StocksProject/Data", T1, k.toString());
		
		HashMap<Cluster, ArrayList<Vector>> canopyClusters = new HashMap<Cluster, ArrayList<Vector>>();
		ArrayList<Vector> allVectors = new ArrayList<Vector>();

		Configuration conf = new Configuration();
		FileSystem fs = FileSystem.get(conf);
		FileStatus[] resultFiles = fs.listStatus(new Path("StocksProject/Data/"));
		
		for (FileStatus file : resultFiles) {
			if (file.getPath().toString().indexOf("Data/canopy") != -1) {
				SequenceFile.Reader reader = new SequenceFile.Reader(fs, file.getPath(), conf);
				Cluster center = new Cluster();
				Vector vector = new Vector();

				while (reader.next(center, vector)) {
					if (!canopyClusters.containsKey(center))
						canopyClusters.put(new Cluster(center), new ArrayList<Vector>());

					// add the vector to its center in the canopies and to the vectors for k-means
					canopyClusters.get(center).add(new Vector(vector));
					allVectors.add(new Vector(vector));
				}
				
				reader.close();
			}
		}
		
		List<Cluster> kMeansCenters = new ArrayList<Cluster>();
		List<Cluster> CanopyCenters = new ArrayList<Cluster>();
		
		int kForCanopy = 0;
		int relativeK;
		int amountOfSpareCenters = getAmountOfSpareCenters(k, canopyClusters, allVectors);

		for (Entry<Cluster, ArrayList<Vector>> entry : canopyClusters.entrySet()) {
			relativeK = (int) Math.round(((double) entry.getValue().size() / allVectors.size()) * k);
			relativeK -= (relativeK > amountOfSpareCenters) ? amountOfSpareCenters : 0;
			
			if (kForCanopy < k) {
				// Run as RelativeK size and pick R-K random centers
				for (int i = 0; i < relativeK; i++) {
					int n = getRandomIndex(entry.getValue().size());
					kMeansCenters.add(new Cluster(entry.getValue().get(n)));
					CanopyCenters.add(entry.getKey());
				}
				
				_randomIndexesList.clear();
				kForCanopy += relativeK;
			}
		} 
	
		Path result = executeKMeans(k, canopyClusters, kMeansCenters, CanopyCenters);
		
		writeOutput(outputDir,result);
	}
	
	@SuppressWarnings("deprecation")
	public static void writeOutput(String outputFile, Path ResultPath) throws IOException, InterruptedException, ClassNotFoundException {
		HashMap<Cluster, ArrayList<Vector>> clusters = new HashMap<Cluster, ArrayList<Vector>>();
		Path outputPath = new Path(outputFile);
		Configuration conf = new Configuration();
		FileSystem fs = FileSystem.get(conf);
		StringBuilder output = new StringBuilder();
		
		if (fs.exists(outputPath))
			fs.delete(outputPath, true);

		{
			SequenceFile.Reader reader = new SequenceFile.Reader(fs, ResultPath, conf);
			Cluster center = new Cluster();
			Vector vector = new Vector();
			
			while (reader.next(center, vector)) {
				if (!clusters.containsKey(center))
					clusters.put(new Cluster(center), new ArrayList<Vector>());
				
				clusters.get(center).add(new Vector(vector));
			}
			
			reader.close();
		}

		for (Cluster cluster : clusters.keySet()) {
			for (Vector vector : clusters.get(cluster))
				output.append(vector.getName());
			
			output.deleteCharAt(output.length() - 1);
			output.append("\n");
		}
		
		FSDataOutputStream writer = fs.create(outputPath);
		writer.writeUTF(output.toString());
		writer.close();
	}
	
	// This method is just in order to cover out ass up in case that the k in canopy will somehow(round double number) bigger than the k in kMeans
	public static int getAmountOfSpareCenters(int k, HashMap<Cluster, ArrayList<Vector>> canopyClusters, ArrayList<Vector> vectorsForKMeans) {
		int tempNumber = 0;
		int nSumOfK = 0;
		int nSelfKAdded = 0;
		
		// Find how much interventions we will need to make for the linearic distribution 
		for (Entry<Cluster, ArrayList<Vector>> entry : canopyClusters.entrySet()) {
			tempNumber = (int) Math.round(((double) entry.getValue().size() / vectorsForKMeans.size()) * k);
			nSumOfK += tempNumber;
			nSelfKAdded += (tempNumber < 1) ? 1 : 0;
		}
		
		return nSelfKAdded + k - nSumOfK;
	}
	 
	public static void executeCanopy(String inputDir, String outputDir, String T1, String k) throws Exception {
		Configuration conf = new Configuration();
		conf.setBoolean("fs.hdfs.impl.disable.cache", true);
		conf.set("T1", T1);
		conf.set("K", k);
		
		FileSystem fs = FileSystem.get(conf);
		Path inputPath = new Path(inputDir);
		Path outputPath = new Path(outputDir);

		if (fs.exists(outputPath)) 
			fs.delete(outputPath, true);

		Job job = new Job(conf);
		job.setJobName("StocksProject-Canopy");
		job.setJarByClass(CanopyMapReduce.class);

		job.setOutputKeyClass(Cluster.class);
		job.setOutputValueClass(Vector.class);

		job.setMapperClass(CanopyMapReduce.Map.class);
		job.setReducerClass(CanopyMapReduce.Reduce.class);
		job.setPartitionerClass(HashPartitioner.class);

		job.setInputFormatClass(TextInputFormat.class);
		job.setOutputFormatClass(SequenceFileOutputFormat.class);

		FileInputFormat.setInputPaths(job, inputPath);
		SequenceFileOutputFormat.setOutputPath(job, outputPath);

		// Submit the job to the cluster and wait for it to finish
		job.waitForCompletion(true);
	}
	
	@SuppressWarnings("deprecation")
	public static Path executeKMeans(int k, HashMap<Cluster, ArrayList<Vector>> canopyClusters, List<Cluster> kMeansCenters, List<Cluster> CanopyCenters) throws IOException, InterruptedException, ClassNotFoundException {
		int iterationIndex = 0;
		long amountOfFinishedCenters = 0;
		Path centers = new Path("StocksProject/clustering/data/centers.txt");
		Path in = new Path("StocksProject/clustering/canopyin");
		Path out = new Path("StocksProject/clustering/canopyout");
		
		Configuration conf = new Configuration();
		conf.set("K", k + "");

		FileSystem fs = FileSystem.get(conf);
		
		if (fs.exists(in)) 
			fs.delete(in, true);
		
		if (fs.exists(centers)) 
			fs.delete(centers, true);

		SequenceFile.Writer centersWriter = SequenceFile.createWriter(fs, conf, centers, Cluster.class, Cluster.class);
		SequenceFile.Writer dataWriter = SequenceFile.createWriter(fs, conf, in, Cluster.class, Vector.class);
		
		// Running over the vectors in the canopy
		for (Entry<Cluster, ArrayList<Vector>> currCanopyCluster : canopyClusters.entrySet()) {
			Cluster canopyParentCenter = currCanopyCluster.getKey();
			
			for (Vector currVector : currCanopyCluster.getValue())
				dataWriter.append(canopyParentCenter, currVector);
		}

		// Running over the clusters
		for (int i = 0; i < kMeansCenters.size(); i++)
			centersWriter.append(CanopyCenters.get(i), kMeansCenters.get(i));
		
		centersWriter.close();
		dataWriter.close();
		
		do {
			if (fs.exists(out)) 
				fs.delete(out, true);
			
			conf.setBoolean("fs.hdfs.impl.disable.cache", true);
			conf.set("num.iteration", iterationIndex + "");
			conf.set("centers.path", centers.toString());
			
			Job job = new Job(conf, "KMeans Clustering" + iterationIndex++);
			job.setMapperClass(KMeansMapReduce.Map.class);
			job.setReducerClass(KMeansMapReduce.Reduce.class);
			job.setJarByClass(KMeansMapReduce.class);
	
			SequenceFileInputFormat.addInputPath(job, in);
			SequenceFileOutputFormat.setOutputPath(job, out);
			
			job.setInputFormatClass(SequenceFileInputFormat.class);
			job.setOutputFormatClass(SequenceFileOutputFormat.class);
			job.setOutputKeyClass(Cluster.class);
			job.setOutputValueClass(Vector.class);
	
			// Submit the job to the cluster and wait for it to finish
			job.waitForCompletion(true);
			
			amountOfFinishedCenters = job.getCounters().findCounter(KMeansMapReduce.Counter.CONVERGED).getValue();
		} while (amountOfFinishedCenters < k);

		return new Path("StocksProject/clustering/canopyout/part-r-00000");
	}
	
	public static int getRandomIndex(Integer nVecSize) {
		int index = (int) Math.floor(Math.random() * nVecSize);
		
		while (_randomIndexesList.contains(index))
			index = (int) Math.floor(Math.random() * nVecSize);

		_randomIndexesList.add(index);
		
		return index;
	}
}
