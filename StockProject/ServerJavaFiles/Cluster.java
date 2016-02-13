package solution;

import java.io.DataInput;
import java.io.DataOutput;
import java.io.IOException;
import org.apache.hadoop.io.WritableComparable;

public class Cluster implements WritableComparable<Cluster> {
	private long _id;
	private Vector _centerVector;
	private boolean _isCovered;
	
	public Cluster() {
		setCenterVector(null);
		setId(-1);
		setIsCovered(false);
	}

	public Cluster(Cluster center) {
		setCenterVector(new Vector(center.getCenterVector()));
		setId(center.getId());
		setIsCovered(center.getIsCovered());
	}

	public Cluster(Vector center, int nClusterId) {
		setCenterVector(center);
		setId(nClusterId);
		setIsCovered(false);
	}
	
	public Cluster(Vector center) {
		setCenterVector(center);
		setId(-1);
		setIsCovered(false);
	}
	
	public Vector getCenterVector() {
		return _centerVector;
	}

	public void setCenterVector(Vector vetor) {
		_centerVector = vetor;
	}

	public long getId() {
		return _id;
	}

	public void setId(long id) {
		_id = id;
	}

	public boolean getIsCovered() {
		return _isCovered;
	}

	public void setIsCovered(boolean covered) {
		_isCovered = covered;
	}
	
	// Checks if both of the clusters converged at the same center
	public boolean isConvergedWithOtherCluster(Cluster c) {
		return compareTo(c) == 0;
	}

	@Override
	public void write(DataOutput out) throws IOException {
		out.writeLong(getId());
		out.writeBoolean(getIsCovered());
		getCenterVector().write(out);
	}
	
	@Override
	public void readFields(DataInput in) throws IOException {
		setId(in.readInt());
		setIsCovered(in.readBoolean());
		setCenterVector(new Vector());
		getCenterVector().readFields(in);
	}

	@Override
	public int compareTo(Cluster otherClusterCenter) {
		return getCenterVector().compareTo(otherClusterCenter.getCenterVector());
	}

	@Override
	public boolean equals(Object o)	{
		return compareTo((Cluster)o) == 0;
	}

	@Override
	public String toString() {
		return "ClusterCenter " + getCenterVector().getName() + " [vector=" + getCenterVector() + "]";
	}

	@Override
	public int hashCode() {
		return toString().hashCode();
	}
}
