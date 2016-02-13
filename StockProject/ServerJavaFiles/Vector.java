package solution;

import java.io.DataInput;
import java.io.DataOutput;
import java.io.IOException;
import java.util.Arrays;
import org.apache.hadoop.io.WritableComparable;

public class Vector implements WritableComparable<Vector> {
	private double[] _elements;
	private String _name = "";
	
	public Vector() {
		super();
	}

	public Vector(Vector otherVector) {
		super();
		int length = otherVector.getElements().length;
		setElements(new double[length]);
		System.arraycopy(otherVector.getElements(), 0, getElements(), 0, length);
		setName(otherVector.getName());
	}
	
	public Vector(int size)	{
		setElements(new double[size]);
		setName("");
	}

	public double[] getElements() {
		return _elements;
	}
	
	public String getName()	{
		return _name;
	}
	
	public void setName(String value) {
		_name = value;
	}
	
	public void setElements(double[] elements) {
		_elements = elements;
	}

	@Override
	public void write(DataOutput out) throws IOException {
		out.writeInt(getElements().length);
		out.writeUTF(getName());
		for (int i = 0; i < getElements().length; i++)
			out.writeDouble(getElements()[i]);
	}

	@Override
	public void readFields(DataInput input) throws IOException {
		int length = input.readInt();
		setName(input.readUTF());
		setElements(new double[length]);
		
		for (int i = 0; i < length; i++)
			getElements()[i] = input.readDouble();
	}

	@Override
	public int compareTo(Vector otherVector) {
		for (int i = 0; i < getElements().length; i++) {
			double c = getElements()[i] - otherVector.getElements()[i];
			
			if (c != 0.0d)
				return (int) c;
		}
		
		return 0;
	}
	
	@Override
	public String toString() {
		return "Vector " + getName() + " [elements=" + Arrays.toString(getElements()) + "]";
	}
}
