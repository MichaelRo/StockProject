package solution;

public class Utils {
	public static final double getDistance(Cluster center, Vector vector) {
		double sum = 0;
		
		for (int i = 0; i < vector.getElements().length; i++)
			sum += Math.abs(center.getCenterVector().getElements()[i] - vector.getElements()[i]);

		return sum / vector.getElements().length;
	}
}
