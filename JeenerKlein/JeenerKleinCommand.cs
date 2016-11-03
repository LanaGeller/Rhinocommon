using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace JeenerKlein
{
    [System.Runtime.InteropServices.Guid("2ef8bbad-999a-4087-8c25-a391cfe10ad8")]
    public class JeenerKleinCommand : Command
    {
        public JeenerKleinCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static JeenerKleinCommand Instance
        {
            get;
            private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "JeenerKlein"; }
        }

        class XYZ
        {
            public double x = 0;
            public double y = 0;
            public double z = 0;
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            RhinoApp.WriteLine("Creating a Jeener's Klein bottle");

            Rhino.Input.Custom.GetPoint gp;

            gp = new Rhino.Input.Custom.GetPoint();
            int S = 3;
            gp.SetCommandPrompt("Please select the parameters S: ");
            Rhino.Input.Custom.OptionInteger intS = new Rhino.Input.Custom.OptionInteger(3, 1, 10);
            gp.AddOptionInteger("intS", ref intS);
            Rhino.Input.GetResult get_S = gp.Get();
            if (gp.CommandResult() != Rhino.Commands.Result.Success) return gp.CommandResult();
            S = intS.CurrentValue;

            gp = new Rhino.Input.Custom.GetPoint();
            int T = 5;
            gp.SetCommandPrompt("Please select the parameters the parameter T: ");
            Rhino.Input.Custom.OptionInteger intT = new Rhino.Input.Custom.OptionInteger(5, 1, 50);
            gp.AddOptionInteger("intT", ref intT);
            Rhino.Input.GetResult get_T = gp.Get();
            if (gp.CommandResult() != Rhino.Commands.Result.Success) return gp.CommandResult();
            T = intT.CurrentValue;            

            Point3d CenterPoint;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the center point");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No center point was selected.");
                    return getPointAction.CommandResult();
                }
                CenterPoint = getPointAction.Point();
            }

            Point3d RadiusPoint;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the radius point");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No radius point was selected.");
                    return getPointAction.CommandResult();
                }
                RadiusPoint = getPointAction.Point();
            }

            Line RadiusLine = new Line(CenterPoint, RadiusPoint);
            double Radius = RadiusLine.Length;

            int N = 100;
            int M = 50;

            double deltaU = 2 * Math.PI / N;
            double deltaV = 2 * Math.PI / M;

            Rhino.Geometry.Mesh MainMesh = new Rhino.Geometry.Mesh();

            List<XYZ> TemplatePoints = new List<XYZ> { };

            int i = 0;
            int j = 0;

            double MaxX = 0;
            double MaxY = 0;
            double MaxRxy = 0;

            while (i <= N)
            {
                j = 0;
                while (j <= M)
                {
                    double u = i * deltaU;
                    double v = j * deltaV;
                    double w = 0.25 * (S + 1) * Math.Cos((S + 1) * u + Math.PI / T) + Math.Sqrt(2);

                    double x = S * Math.Cos(u) + Math.Cos(S * u) - w * Math.Sin(0.5 * (S - 1) * u) * Math.Cos(v);
                    double y = S * Math.Sin(u) - Math.Sin(S * u) - w * Math.Cos(0.5 * (S - 1) * u) * Math.Cos(v);
                    double z = w * Math.Sin(v);

                    XYZ xyz = new XYZ();
                    xyz.x = x;
                    xyz.y = y;
                    xyz.z = z;

                    double Rxy = Math.Sqrt(x * x + y * y);

                    MaxX = Math.Max(MaxX, x);
                    MaxY = Math.Max(MaxY, y);
                    MaxRxy = Math.Max(MaxRxy, Rxy);

                    TemplatePoints.Add(xyz);
                    
                    j++;
                }
                i++;
            }

            double MaxR = Math.Max(MaxRxy, Math.Max(MaxX, MaxY));
            double Scale = Radius / MaxR;

            foreach (XYZ xyz in TemplatePoints)
            {
                xyz.x = Scale * xyz.x;
                xyz.y = Scale * xyz.y;
                xyz.z = Scale * xyz.z;
                MainMesh.Vertices.Add(xyz.x + CenterPoint.X, xyz.y + CenterPoint.Y, xyz.z + CenterPoint.Z);
            }

            i = 0;
            j = 0;

            while (i <= N - 1)
            {
                j = 0;
                while (j <= M - 1)
                {
                    MainMesh.Faces.AddFace(i * (M + 1) + j, 
                                           i * (M + 1) + j + 1, 
                                          (i + 1) * (M + 1) + j + 1, 
                                          (i + 1) * (M + 1) + j);
                    j++;
                }
                i++;
            }
            
            MainMesh.Normals.ComputeNormals();
            MainMesh.Compact();
            if (doc.Objects.AddMesh(MainMesh) != Guid.Empty)
            {
                doc.Views.Redraw();
                RhinoApp.WriteLine("Jeener's Klein bottle with S={0}, T={1} has bee built!", S, T);
                return Rhino.Commands.Result.Success;
            }
            return Rhino.Commands.Result.Failure;
        }
    }
}
