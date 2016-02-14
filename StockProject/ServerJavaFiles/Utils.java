package solution;

public class Utils {
	public static final String MAIN_DIRECTORY = "StocksProject/";
	public static final String CANOPY_OUTPUT_DIRECTORY = MAIN_DIRECTORY + "Data/";
	public static final String CANOPY_FILE_PREFIX = "Canopy";
	public static final String KMEANS_OUTPUT_DIRECTORY = MAIN_DIRECTORY + "Clustering/";
	
	public static final double getDistance(Cluster center, Vector vector) {
		double sum = 0;
		
		for (int i = 0; i < vector.getElements().length; i++)
			sum += Math.abs(center.getCenterVector().getElements()[i] - vector.getElements()[i]);

		return sum / vector.getElements().length;
	}
}
