using System.Xml.Serialization;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System;

namespace LXFPartListCreator
{
    using static XmlSchemaForm;


    // AUTO-GENERATED from 'lxfml.xsd'


    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Camera
    {
        [XmlAttribute]
        public string refID { set; get; }
        [XmlAttribute]
        public string fieldOfView { set; get; }
        [XmlAttribute]
        public string distance { set; get; }
        [XmlAttribute]
        public string transformation { set; get; }
        [XmlAttribute]
        public string cameraRef { set; get; }
    }
    
    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Step
    {
        [XmlElement("PartRef", Form = Unqualified)]
        public StepPartRef[] PartRef { set; get; }
        [XmlElement("Camera")]
        public Camera[] Camera { set; get; }
        [XmlElement("Step")]
        public Step[] Step1 { set; get; }
        [XmlAttribute]
        public string name { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class StepPartRef
    {
        [XmlAttribute]
        public string partRef { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true), XmlRoot(Namespace = "", IsNullable = false)]
    public partial class LXFML
    {
        [XmlElement("Meta", Form = Unqualified)]
        public LXFMLMeta[] Meta { set; get; }
        [XmlArray(Form = Unqualified), XmlArrayItem("Camera", typeof(Camera), IsNullable = false)]
        public Camera[] Cameras { set; get; }
        [XmlElement("Bricks", Form = Unqualified)]
        public LXFMLBricks[] Bricks { set; get; }
        [XmlArray(Form = Unqualified), XmlArrayItem("RigidSystem", typeof(LXFMLRigidSystemsRigidSystem), Form = Unqualified, IsNullable = false)]
        public LXFMLRigidSystemsRigidSystem[] RigidSystems { set; get; }
        [XmlArray(Form = Unqualified), XmlArrayItem("GroupSystem", typeof(LXFMLGroupSystemsGroupSystemGroup[]), Form = Unqualified, IsNullable = false), XmlArrayItem("Group", typeof(LXFMLGroupSystemsGroupSystemGroup), Form = Unqualified, IsNullable = false, NestingLevel = 1)]
        public LXFMLGroupSystemsGroupSystemGroup[][] GroupSystems { set; get; }
        [XmlArray(Form = Unqualified), XmlArrayItem("BuildingInstruction", typeof(LXFMLBuildingInstructionsBuildingInstruction), Form = Unqualified, IsNullable = false)]
        public LXFMLBuildingInstructionsBuildingInstruction[] BuildingInstructions { set; get; }
        [XmlAttribute]
        public string versionMajor { set; get; }
        [XmlAttribute]
        public string versionMinor { set; get; }
        [XmlAttribute]
        public string name { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLMeta
    {
        [XmlElement("Application", Form = Unqualified)]
        public LXFMLMetaApplication[] Application { set; get; }
        [XmlElement("Brand", Form = Unqualified)]
        public LXFMLMetaBrand[] Brand { set; get; }
        [XmlElement("BrickSet", Form = Unqualified)]
        public LXFMLMetaBrickSet[] BrickSet { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLMetaApplication
    {
        [XmlAttribute]
        public string name { set; get; }
        [XmlAttribute]
        public string versionMajor { set; get; }
        [XmlAttribute]
        public string versionMinor { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLMetaBrand
    {
        [XmlAttribute]
        public string name { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLMetaBrickSet
    {
        [XmlAttribute]
        public string version { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLBricks
    {
        [XmlElement("Brick", Form = Unqualified)]
        public LXFMLBricksBrick[] Brick { set; get; }
        [XmlAttribute]
        public string cameraRef { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLBricksBrick
    {
        [XmlElement("Part", Form = Unqualified)]
        public LXFMLBricksBrickPart[] Part { set; get; }
        [XmlAttribute]
        public string refID { set; get; }
        [XmlAttribute]
        public string designID { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLBricksBrickPart
    {
        [XmlElement("Bone", Form = Unqualified)]
        public LXFMLBricksBrickPartBone[] Bone { set; get; }
        [XmlAttribute]
        public string refID { set; get; }
        [XmlAttribute]
        public string designID { set; get; }
        [XmlAttribute]
        public string materials { set; get; }
        [XmlAttribute]
        public string decoration { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLBricksBrickPartBone
    {
        [XmlAttribute]
        public string refID { set; get; }
        [XmlAttribute]
        public string transformation { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLRigidSystemsRigidSystem
    {
        [XmlElement("Rigid", Form = Unqualified)]
        public LXFMLRigidSystemsRigidSystemRigid[] Rigid { set; get; }
        [XmlElement("Joint", Form = Unqualified)]
        public LXFMLRigidSystemsRigidSystemJoint[] Joint { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLRigidSystemsRigidSystemRigid
    {
        [XmlAttribute]
        public string refID { set; get; }
        [XmlAttribute]
        public string transformation { set; get; }
        [XmlAttribute]
        public string boneRefs { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLRigidSystemsRigidSystemJoint
    {
        [XmlElement("RigidRef", Form = Unqualified)]
        public LXFMLRigidSystemsRigidSystemJointRigidRef[] RigidRef { set; get; }
        [XmlAttribute]
        public string type { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLRigidSystemsRigidSystemJointRigidRef
    {
        [XmlAttribute]
        public string rigidRef { set; get; }
        [XmlAttribute]
        public string a { set; get; }
        [XmlAttribute]
        public string z { set; get; }
        [XmlAttribute]
        public string t { set; get; }
    }
    
    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLGroupSystemsGroupSystemGroup
    {
        [XmlAttribute]
        public string transformation { set; get; }
        [XmlAttribute]
        public string pivot { set; get; }
        [XmlAttribute]
        public string partRefs { set; get; }
    }

    [GeneratedCode("xsd", "4.6.1055.0"), Serializable, DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public partial class LXFMLBuildingInstructionsBuildingInstruction
    {
        [XmlElement("Camera")]
        public Camera[] Camera { set; get; }
        [XmlElement("Step")]
        public Step[] Step { set; get; }
        [XmlAttribute]
        public string name { set; get; }
    }
}
