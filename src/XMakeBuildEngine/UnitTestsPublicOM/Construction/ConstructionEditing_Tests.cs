// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Tests for editing through the construction model.</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using InvalidProjectFileException = Microsoft.Build.Exceptions.InvalidProjectFileException;

namespace Microsoft.Build.UnitTests.OM.Construction
{
    /// <summary>
    /// Tests for editing through the construction model
    /// </summary>
    [TestClass]
    public class ConstructionEditing_Tests
    {
        /// <summary>
        /// Add a target through the convenience method
        /// </summary>
        [TestMethod]
        public void AddTargetConvenience()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.AddTarget("t");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
</Project>");

            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(1, project.Count);
            Assert.AreEqual(0, target.Count);
            Assert.AreEqual(1, Helpers.Count(project.Children));
            Assert.AreEqual(0, Helpers.Count(target.Children));
            Assert.AreEqual(null, project.Parent);
            Assert.AreEqual(project, target.Parent);
        }

        /// <summary>
        /// Simple add a target
        /// </summary>
        [TestMethod]
        public void AppendTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            Helpers.ClearDirtyFlag(project);
            ProjectTargetElement target = project.CreateTargetElement("t");
            Assert.AreEqual(false, project.HasUnsavedChanges);

            project.AppendChild(target);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(1, project.Count);
        }

        /// <summary>
        /// Append two targets
        /// </summary>
        [TestMethod]
        public void AppendTargetTwice()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.CreateTargetElement("t");
            ProjectTargetElement target2 = project.CreateTargetElement("t2");

            project.AppendChild(target1);
            project.AppendChild(target2);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
  <Target Name=""t2"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);

            Assert.AreEqual(2, project.Count);
            var targets = Helpers.MakeList(project.Targets);
            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(target1, targets[0]);
            Assert.AreEqual(target2, targets[1]);
        }

        /// <summary>
        /// Add node created from different project with AppendChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAddFromDifferentProject_AppendChild()
        {
            ProjectRootElement project1 = ProjectRootElement.Create();
            ProjectRootElement project2 = ProjectRootElement.Create();
            ProjectTargetElement target = project1.CreateTargetElement("t");
            project2.AppendChild(target);
        }

        /// <summary>
        /// Add node created from different project with PrependChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAddFromDifferentProject_PrependChild()
        {
            ProjectRootElement project1 = ProjectRootElement.Create();
            ProjectRootElement project2 = ProjectRootElement.Create();
            ProjectTargetElement target = project1.CreateTargetElement("t");
            project2.PrependChild(target);
        }

        /// <summary>
        /// Add node created from different project with InsertBeforeChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAddFromDifferentProject_InsertBefore()
        {
            ProjectRootElement project1 = ProjectRootElement.Create();
            ProjectRootElement project2 = ProjectRootElement.Create();
            ProjectTargetElement target1 = project1.CreateTargetElement("t");
            ProjectTargetElement target2 = project2.AddTarget("t2");
            project2.InsertBeforeChild(target2, target1);
        }

        /// <summary>
        /// Add node created from different project with InsertAfterChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAddFromDifferentProject_InsertAfter()
        {
            ProjectRootElement project1 = ProjectRootElement.Create();
            ProjectRootElement project2 = ProjectRootElement.Create();
            ProjectTargetElement target1 = project1.CreateTargetElement("t");
            ProjectTargetElement target2 = project2.AddTarget("t2");
            project2.InsertAfterChild(target2, target1);
        }

        /// <summary>
        /// Become direct child of self with AppendChild
        /// (This is prevented anyway because the parent is an invalid type.)
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidBecomeChildOfSelf_AppendChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();

            choose.AppendChild(choose);
        }

        /// <summary>
        /// Become grandchild of self with AppendChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidBecomeGrandChildOfSelf_AppendChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            project.AppendChild(choose);
            choose.AppendChild(when);
            when.AppendChild(choose);
        }

        /// <summary>
        /// Become grandchild of self with PrependChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidBecomeGrandChildOfSelf_PrependChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            project.AppendChild(choose);
            choose.AppendChild(when);
            when.PrependChild(choose);
        }

        /// <summary>
        /// Become grandchild of self with InsertBeforeChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidBecomeGrandChildOfSelf_InsertBefore()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose1 = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            ProjectChooseElement choose2 = project.CreateChooseElement();
            project.AppendChild(choose1);
            choose1.AppendChild(when);
            when.PrependChild(choose2);
            when.InsertBeforeChild(choose1, choose2);
        }

        /// <summary>
        /// Become grandchild of self with InsertAfterChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidBecomeGrandChildOfSelf_InsertAfter()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose1 = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            ProjectChooseElement choose2 = project.CreateChooseElement();
            project.AppendChild(choose1);
            choose1.AppendChild(when);
            when.PrependChild(choose2);
            when.InsertAfterChild(choose1, choose2);
        }

        /// <summary>
        /// Attempt to reparent with AppendChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAlreadyParented_AppendChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.AddTarget("t");

            project.AppendChild(target);
        }

        /// <summary>
        /// Attempt to reparent with PrependChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAlreadyParented_PrependChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.AddTarget("t");

            project.PrependChild(target);
        }

        /// <summary>
        /// Attempt to reparent with InsertBeforeChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAlreadyParented_InsertBefore()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.InsertBeforeChild(target1, target2);
        }

        /// <summary>
        /// Attempt to reparent with InsertAfterChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAlreadyParented_InsertAfter()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.InsertAfterChild(target1, target2);
        }

        /// <summary>
        /// Attempt to add to unparented parent with AppendChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidParentNotParented_AppendChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectTaskElement task = project.CreateTaskElement("tt");

            target.AppendChild(task);
        }

        /// <summary>
        /// Attempt to add to unparented parent with PrependChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidParentNotParented_PrependChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectTaskElement task = project.CreateTaskElement("tt");

            target.PrependChild(task);
        }

        /// <summary>
        /// Attempt to add to unparented parent with InsertBeforeChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidParentNotParented_InsertBefore()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectTaskElement task1 = project.CreateTaskElement("tt");
            ProjectTaskElement task2 = project.CreateTaskElement("tt");

            target.InsertBeforeChild(task2, task1);
        }

        /// <summary>
        /// Attempt to add to unparented parent with InsertAfterChild
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidParentNotParented_InsertAfter()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectTaskElement task1 = project.CreateTaskElement("tt");
            ProjectTaskElement task2 = project.CreateTaskElement("tt");

            target.InsertAfterChild(task2, task1);
        }

        /// <summary>
        /// Setting attributes on a target should be reflected in the XML
        /// </summary>
        [TestMethod]
        public void AppendTargetSetAllAttributes()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");

            project.AppendChild(target);
            target.Inputs = "i";
            target.Outputs = "o";
            target.DependsOnTargets = "d";
            target.Condition = "c";

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" Inputs=""i"" Outputs=""o"" DependsOnTargets=""d"" Condition=""c"" />
</Project>");

            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Clearing attributes on a target should be reflected in the XML
        /// </summary>
        [TestMethod]
        public void AppendTargetClearAttributes()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");

            project.AppendChild(target);
            target.Inputs = "i";
            target.Outputs = "o";
            target.Inputs = String.Empty;

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" Outputs=""o"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Prepend item group
        /// </summary>
        [TestMethod]
        public void PrependItemGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();

            project.PrependChild(itemGroup);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup />
</Project>");

            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);

            Assert.AreEqual(1, project.Count);
            var children = Helpers.MakeList(project.Children);
            Assert.AreEqual(1, children.Count);
            Assert.AreEqual(itemGroup, children[0]);
        }

        /// <summary>
        /// Insert target before
        /// </summary>
        [TestMethod]
        public void InsertTargetBefore()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectTargetElement target = project.CreateTargetElement("t");

            project.PrependChild(itemGroup);
            project.InsertBeforeChild(target, itemGroup);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
  <ItemGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);

            Assert.AreEqual(2, project.Count);
            var children = Helpers.MakeList(project.Children);
            Assert.AreEqual(2, children.Count);
            Assert.AreEqual(target, children[0]);
            Assert.AreEqual(itemGroup, children[1]);
        }

        /// <summary>
        /// InsertBeforeChild with a null reference node should be the same as calling AppendChild.
        /// This matches XmlNode behavior.
        /// </summary>
        [TestMethod]
        public void InsertTargetBeforeNullEquivalentToAppendChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectTargetElement target = project.CreateTargetElement("t");

            project.PrependChild(itemGroup);
            project.InsertBeforeChild(target, null);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup />
  <Target Name=""t"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// InsertAfterChild with a null reference node should be the same as calling PrependChild.
        /// This matches XmlNode behavior.
        /// </summary>
        [TestMethod]
        public void InsertTargetAfterNullEquivalentToPrependChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectTargetElement target = project.CreateTargetElement("t");

            project.PrependChild(itemGroup);
            project.InsertAfterChild(target, null);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
  <ItemGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Insert target before and after a reference
        /// </summary>
        [TestMethod]
        public void InsertTargetBeforeAndTargetAfter()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectTargetElement target1 = project.CreateTargetElement("t");
            ProjectTargetElement target2 = project.CreateTargetElement("t2");

            project.PrependChild(itemGroup);
            project.InsertBeforeChild(target1, itemGroup);
            project.InsertAfterChild(target2, itemGroup);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
  <ItemGroup />
  <Target Name=""t2"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);

            Assert.AreEqual(3, project.Count);
            var children = Helpers.MakeList(project.Children);
            Assert.AreEqual(3, children.Count);
            Assert.AreEqual(target1, children[0]);
            Assert.AreEqual(itemGroup, children[1]);
            Assert.AreEqual(target2, children[2]);
        }

        /// <summary>
        /// Insert before when no children
        /// </summary>
        [TestMethod]
        public void InsertTargetBeforeNothing()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.CreateTargetElement("t");

            project.InsertBeforeChild(target1, null);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
</Project>");

            Assert.AreEqual(1, project.Count);
            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Insert after when no children
        /// </summary>
        [TestMethod]
        public void InsertTargetAfterNothing()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");

            project.InsertAfterChild(target, null);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
</Project>");

            Assert.AreEqual(1, project.Count);
            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Insert task in target
        /// </summary>
        [TestMethod]
        public void InsertTaskInTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectTaskElement task = project.CreateTaskElement("tt");

            project.AppendChild(target);
            target.AppendChild(task);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"">
    <tt />
  </Target>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add a task through the convenience method
        /// </summary>
        [TestMethod]
        public void AddTaskConvenience()
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectTargetElement target = project.AddTarget("t");
            ProjectTaskElement task = target.AddTask("tt");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"">
    <tt />
  </Target>
</Project>");

            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Attempt to insert project in target
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAttemptToAddProjectToTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");

            target.AppendChild(project);
        }

        /// <summary>
        /// Attempt to insert item in target
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAttemptToAddItemToTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectItemElement item = project.CreateItemElement("i");

            project.AppendChild(target);
            target.AppendChild(item);
        }

        /// <summary>
        /// Attempt to insert item without include in itemgroup in project
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAttemptToAddItemWithoutIncludeToItemGroupInProject()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectItemElement item = project.CreateItemElement("i");

            project.AppendChild(itemGroup);
            itemGroup.AppendChild(item);
        }

        /// <summary>
        /// Attempt to insert item with remove in itemgroup in project
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAttemptToAddItemWithRemoveToItemGroupInProject()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectItemElement item = project.CreateItemElement("i");
            item.Remove = "r";

            project.AppendChild(itemGroup);
            itemGroup.AppendChild(item);
        }

        /// <summary>
        /// Add item without include in itemgroup in target
        /// </summary>
        [TestMethod]
        public void AddItemWithoutIncludeToItemGroupInTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectItemElement item = project.CreateItemElement("i");

            project.AppendChild(target);
            target.AppendChild(itemGroup);
            itemGroup.AppendChild(item);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"">
    <ItemGroup>
      <i />
    </ItemGroup>
  </Target>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add item with remove in itemgroup in target
        /// </summary>
        [TestMethod]
        public void AddItemWithRemoveToItemGroupInTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            ProjectItemGroupElement itemGroup = project.CreateItemGroupElement();
            ProjectItemElement item = project.CreateItemElement("i");
            item.Remove = "r";

            project.AppendChild(target);
            target.AppendChild(itemGroup);
            itemGroup.AppendChild(item);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"">
    <ItemGroup>
      <i Remove=""r"" />
    </ItemGroup>
  </Target>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Remove a target
        /// </summary>
        [TestMethod]
        public void RemoveSingleChildTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.AddTarget("t");
            Helpers.ClearDirtyFlag(project);

            project.RemoveChild(target);

            string expected = ObjectModelHelpers.CleanupFileContents(@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"" />");

            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(0, Helpers.Count(project.Children));
        }

        /// <summary>
        /// Attempt to remove a child that is not parented
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidRemoveUnparentedChild()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target = project.CreateTargetElement("t");
            project.RemoveChild(target);
        }

        /// <summary>
        /// Attempt to remove a child that is parented by something in another project
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidRemoveChildFromOtherProject()
        {
            ProjectRootElement project1 = ProjectRootElement.Create();
            ProjectTargetElement target = project1.CreateTargetElement("t");
            ProjectRootElement project2 = ProjectRootElement.Create();

            project2.RemoveChild(target);
        }

        /// <summary>
        /// Attempt to remove a child that is parented by something else in the same project
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidRemoveChildFromOtherParent()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup1 = project.CreateItemGroupElement();
            ProjectItemGroupElement itemGroup2 = project.CreateItemGroupElement();
            ProjectItemElement item = project.CreateItemElement("i");
            itemGroup1.AppendChild(item);

            itemGroup2.RemoveChild(item);
        }

        /// <summary>
        /// Attempt to add an Otherwise before a When
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidOtherwiseBeforeWhen()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            ProjectOtherwiseElement otherwise = project.CreateOtherwiseElement();

            project.AppendChild(choose);
            choose.AppendChild(when);
            choose.InsertBeforeChild(otherwise, when);
        }

        /// <summary>
        /// Attempt to add an Otherwise after another
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidOtherwiseAfterOtherwise()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            project.AppendChild(choose);
            choose.AppendChild(project.CreateWhenElement("c"));
            choose.AppendChild(project.CreateOtherwiseElement());
            choose.AppendChild(project.CreateOtherwiseElement());
        }

        /// <summary>
        /// Attempt to add an Otherwise before another
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidOtherwiseBeforeOtherwise()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            project.AppendChild(choose);
            choose.AppendChild(project.CreateWhenElement("c"));
            choose.AppendChild(project.CreateOtherwiseElement());
            choose.InsertAfterChild(project.CreateOtherwiseElement(), choose.FirstChild);
        }

        /// <summary>
        /// Attempt to add a When after an Otherwise
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidWhenAfterOtherwise()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            ProjectOtherwiseElement otherwise = project.CreateOtherwiseElement();

            project.AppendChild(choose);
            choose.AppendChild(otherwise);
            choose.InsertAfterChild(when, otherwise);
        }

        /// <summary>
        /// Add When before Otherwise
        /// </summary>
        [TestMethod]
        public void WhenBeforeOtherwise()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            ProjectOtherwiseElement otherwise = project.CreateOtherwiseElement();

            project.AppendChild(choose);
            choose.AppendChild(when);
            choose.AppendChild(otherwise);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Choose>
    <When Condition=""c"" />
    <Otherwise />
  </Choose>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(1, Helpers.Count(project.Children));
            Assert.AreEqual(2, Helpers.Count(choose.Children));
        }

        /// <summary>
        /// Remove a target that is last in a list
        /// </summary>
        [TestMethod]
        public void RemoveLastInList()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.RemoveChild(target2);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t1"" />
</Project>");

            Assert.AreEqual(1, project.Count);
            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(1, Helpers.Count(project.Children));
            Assert.AreEqual(target1, Helpers.GetFirst(project.Children));
        }

        /// <summary>
        /// Remove a target that is first in a list
        /// </summary>
        [TestMethod]
        public void RemoveFirstInList()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.RemoveChild(target1);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t2"" />
</Project>");

            Assert.AreEqual(1, project.Count);
            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(1, Helpers.Count(project.Children));
            Assert.AreEqual(target2, Helpers.GetFirst(project.Children));
        }

        /// <summary>
        /// Remove all children when there are some
        /// </summary>
        [TestMethod]
        public void RemoveAllChildrenSome()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.RemoveAllChildren();

            Assert.AreEqual(0, project.Count);
            Assert.AreEqual(null, target1.Parent);
            Assert.AreEqual(null, target2.Parent);
        }

        /// <summary>
        /// Remove all children when there aren't any. Shouldn't fail.
        /// </summary>
        [TestMethod]
        public void RemoveAllChildrenNone()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");

            target1.RemoveAllChildren();

            Assert.AreEqual(0, target1.Count);
        }

        /// <summary>
        /// Remove and re-insert a node
        /// </summary>
        [TestMethod]
        public void RemoveReinsertHasSiblingAppend()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.RemoveChild(target1);
            project.AppendChild(target1);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t2"" />
  <Target Name=""t1"" />
</Project>");

            Assert.AreEqual(2, project.Count);
            Assert.AreEqual(true, project.HasUnsavedChanges);
            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(2, Helpers.Count(project.Children));
            Assert.AreEqual(target2, Helpers.GetFirst(project.Children));
        }

        /// <summary>
        /// Remove and re-insert a node
        /// </summary>
        [TestMethod]
        public void RemoveReinsertHasSiblingPrepend()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.RemoveChild(target1);
            project.PrependChild(target1);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t1"" />
  <Target Name=""t2"" />
</Project>");

            Assert.AreEqual(2, project.Count);
            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Remove and re-insert a node
        /// </summary>
        [TestMethod]
        public void RemoveReinsertTwoChildrenAppend()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");

            project.RemoveAllChildren();
            project.AppendChild(target1);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t1"" />
</Project>");

            Assert.AreEqual(1, project.Count);
            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Remove and re-insert a node with no siblings using PrependChild
        /// </summary>
        [TestMethod]
        public void RemoveLonelyReinsertPrepend()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");

            project.RemoveChild(target1);
            project.PrependChild(target1);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t1"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Remove and re-insert a node with no siblings using AppendChild
        /// </summary>
        [TestMethod]
        public void RemoveLonelyReinsertAppend()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");

            project.RemoveAllChildren();
            project.AppendChild(target1);

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t1"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Test the AddPropertyGroup convenience method
        /// It adds after the last existing property group, if any; otherwise
        /// at the start of the project.
        /// </summary>
        [TestMethod]
        public void AddPropertyGroup_NoExistingPropertyGroups()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddTarget("t1");
            project.AddTarget("t2");

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup />
  <Target Name=""t1"" />
  <Target Name=""t2"" />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(3, Helpers.Count(project.Children));
            Assert.AreEqual(propertyGroup, Helpers.GetFirst(project.Children));
        }

        /// <summary>
        /// Test the AddPropertyGroup convenience method
        /// It adds after the last existing property group, if any; otherwise
        /// at the start of the project.
        /// </summary>
        [TestMethod]
        public void AddPropertyGroup_ExistingPropertyGroups()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectTargetElement target1 = project.AddTarget("t1");
            ProjectTargetElement target2 = project.AddTarget("t2");
            ProjectPropertyGroupElement propertyGroup1 = project.CreatePropertyGroupElement();
            ProjectPropertyGroupElement propertyGroup2 = project.CreatePropertyGroupElement();

            project.InsertAfterChild(propertyGroup1, target1);
            project.InsertAfterChild(propertyGroup2, target2);

            ProjectPropertyGroupElement propertyGroup3 = project.AddPropertyGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t1"" />
  <PropertyGroup />
  <Target Name=""t2"" />
  <PropertyGroup />
  <PropertyGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(5, Helpers.Count(project.Children));
            Assert.AreEqual(propertyGroup3, Helpers.GetLast(project.Children));
        }

        /// <summary>
        /// Add an item group to an empty project
        /// </summary>
        [TestMethod]
        public void AddItemGroup_NoExistingElements()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item group to a project with an existing item group; should add 2nd
        /// </summary>
        [TestMethod]
        public void AddItemGroup_OneExistingItemGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemGroup();
            ProjectItemGroupElement itemGroup2 = project.AddItemGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup />
  <ItemGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(itemGroup2, Helpers.GetLast(project.ItemGroups));
        }

        /// <summary>
        /// Add an item group to a project with an existing property group; should add 2nd
        /// </summary>
        [TestMethod]
        public void AddItemGroup_OneExistingPropertyGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddPropertyGroup();
            project.AddItemGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup />
  <ItemGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item group to a project with an existing property group and item group;
        /// should add after the item group
        /// </summary>
        [TestMethod]
        public void AddItemGroup_ExistingItemGroupAndPropertyGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemGroup();
            project.AppendChild(project.CreatePropertyGroupElement());
            ProjectItemGroupElement itemGroup2 = project.AddItemGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup />
  <ItemGroup />
  <PropertyGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(itemGroup2, Helpers.GetLast(project.ItemGroups));
        }

        /// <summary>
        /// Add an item group to a project with an existing target;
        /// should add at the end
        /// </summary>
        [TestMethod]
        public void AddItemGroup_ExistingTarget()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddTarget("t");
            project.AddItemGroup();

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Target Name=""t"" />
  <ItemGroup />
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to an empty project
        /// should add to new item group
        /// </summary>
        [TestMethod]
        public void AddItem_EmptyProject()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemElement item = project.AddItem("i", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project that only has an empty item group,
        /// should reuse that group
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingEmptyItemGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemGroup();
            ProjectItemElement item = project.AddItem("i", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project that only has an empty item group,
        /// should reuse that group, unless it has a condition
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingEmptyItemGroupWithCondition()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.AddItemGroup();
            itemGroup.Condition = "c";
            ProjectItemElement item = project.AddItem("i", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup Condition=""c"" />
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project that only has an item group with items of a different type,
        /// and an empty item group, should reuse that group
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingEmptyItemGroupPlusItemGroupOfWrongType()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.AddItemGroup();
            itemGroup.AddItem("h", "h1");
            project.AddItemGroup();
            ProjectItemElement item = project.AddItem("i", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <h Include=""h1"" />
  </ItemGroup>
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project that only has an item group with items of a different type,
        /// and an empty item group above it, should reuse the empty group
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingEmptyItemGroupPlusItemGroupOfWrongTypeBelow()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemGroup();
            ProjectItemGroupElement itemGroup = project.AddItemGroup();
            itemGroup.AddItem("h", "h1");
            ProjectItemElement item = project.AddItem("i", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
  <ItemGroup>
    <h Include=""h1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(item, Helpers.GetFirst(Helpers.GetFirst(project.ItemGroups).Items));
        }

        /// <summary>
        /// Add an item to a project with a single item group with existing items
        /// of a different item type; should add in alpha order of item type
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingItemGroupWithItemsOfDifferentItemType()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItem("i", "i1");
            project.AddItem("j", "j1");
            project.AddItem("h", "h1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
  <ItemGroup>
    <j Include=""j1"" />
  </ItemGroup>
  <ItemGroup>
    <h Include=""h1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project with a single item group with existing items of
        /// same item type; should add in alpha order of itemspec
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingItemGroupWithItemsOfSameItemType()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItem("i", "i1");
            project.AddItem("i", "j1");
            project.AddItem("i", "h1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""h1"" />
    <i Include=""i1"" />
    <i Include=""j1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project with an existing item group with items of a different
        /// type; should create a new item group
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingItemGroupWithDifferentItemType()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItem("i", "i1");
            project.AddItem("j", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
  <ItemGroup>
    <j Include=""i1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item to a project with a single item group with existing items of
        /// various item types and item specs; should add in alpha order of item type,
        /// then item spec, keeping different item specs in different groups; different
        /// item groups are not mutally sorted
        /// </summary>
        [TestMethod]
        public void AddItem_ExistingItemGroupWithVariousItems()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItem("i", "i1");
            project.AddItem("i", "j1");
            project.AddItem("j", "h1");
            project.AddItem("i", "h1");
            project.AddItem("h", "j1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""h1"" />
    <i Include=""i1"" />
    <i Include=""j1"" />
  </ItemGroup>
  <ItemGroup>
    <j Include=""h1"" />
  </ItemGroup>
  <ItemGroup>
    <h Include=""j1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Adding an item that's identical to an existing one should add it again and not skip
        /// </summary>
        [TestMethod]
        public void AddItem_Duplicate()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItem("i", "i1");
            project.AddItem("i", "i1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
    <i Include=""i1"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Adding items to when and Otherwise
        /// </summary>
        [TestMethod]
        public void AddItemToWhereOtherwise()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectChooseElement choose = project.CreateChooseElement();
            ProjectWhenElement when = project.CreateWhenElement("c");
            ProjectItemGroupElement ig1 = project.CreateItemGroupElement();
            project.AppendChild(choose);
            choose.AppendChild(when);
            when.AppendChild(ig1);
            ig1.AddItem("j", "j1");

            ProjectOtherwiseElement otherwise = project.CreateOtherwiseElement();
            ProjectItemGroupElement ig2 = project.CreateItemGroupElement();
            choose.AppendChild(otherwise);
            otherwise.AppendChild(ig2);
            ig2.AddItem("j", "j2");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <Choose>
    <When Condition=""c"">
      <ItemGroup>
        <j Include=""j1"" />
      </ItemGroup>
    </When>
    <Otherwise>
      <ItemGroup>
        <j Include=""j2"" />
      </ItemGroup>
    </Otherwise>
  </Choose>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Adding items to a specific item group should order them by item type and item spec
        /// </summary>
        [TestMethod]
        public void AddItemToItemGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemGroupElement itemGroup = project.AddItemGroup();
            itemGroup.AddItem("j", "j1");
            itemGroup.AddItem("i", "i1");
            itemGroup.AddItem("h", "h1");
            itemGroup.AddItem("j", "j2");
            itemGroup.AddItem("j", "j0");
            itemGroup.AddItem("h", "h0");
            itemGroup.AddItem("g", "zzz");
            itemGroup.AddItem("k", "aaa");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <g Include=""zzz"" />
    <h Include=""h0"" />
    <h Include=""h1"" />
    <i Include=""i1"" />
    <j Include=""j0"" />
    <j Include=""j1"" />
    <j Include=""j2"" />
    <k Include=""aaa"" />
  </ItemGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item definition to an empty project
        /// should add to new item definition group
        /// </summary>
        [TestMethod]
        public void AddItemDefinition_EmptyProject()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemDefinitionElement itemDefinition = project.AddItemDefinition("i");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemDefinitionGroup>
    <i />
  </ItemDefinitionGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(itemDefinition, Helpers.GetFirst(Helpers.GetFirst(project.ItemDefinitionGroups).ItemDefinitions));
        }

        /// <summary>
        /// Add an item definition to a project with a single empty item definition group;
        /// should create another, because it doesn't have any items of the same type
        /// </summary>
        [TestMethod]
        public void AddItemDefinition_ExistingItemDefinitionGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemDefinitionGroup();
            project.AddItemDefinition("i");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemDefinitionGroup />
  <ItemDefinitionGroup>
    <i />
  </ItemDefinitionGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item definition to a project with a single empty item definition group with a condition;
        /// should create a new one after
        /// </summary>
        [TestMethod]
        public void AddItemDefinition_ExistingItemDefinitionGroupWithCondition()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectItemDefinitionGroupElement itemGroup = project.AddItemDefinitionGroup();
            itemGroup.Condition = "c";
            project.AddItemDefinition("i");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemDefinitionGroup Condition=""c"" />
  <ItemDefinitionGroup>
    <i />
  </ItemDefinitionGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add an item definition to a project with a single item definitiongroup with existing items of
        /// same item type; should add in same one
        /// </summary>
        [TestMethod]
        public void AddItemDefinition_ExistingItemDefinitionGroupWithItemsOfSameItemType()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemDefinition("i");
            project.AddItemDefinition("i");
            ProjectItemDefinitionElement last = project.AddItemDefinition("i");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemDefinitionGroup>
    <i />
    <i />
    <i />
  </ItemDefinitionGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(last, Helpers.GetLast(Helpers.GetFirst(project.ItemDefinitionGroups).ItemDefinitions));
        }

        /// <summary>
        /// Add an item definition to a project with an existing item definition group with items of a different
        /// type; should create a new item definition group
        /// </summary>
        [TestMethod]
        public void AddItemDefinition_ExistingItemDefinitionGroupWithDifferentItemType()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddItemDefinition("i");
            project.AddItemDefinition("j");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemDefinitionGroup>
    <i />
  </ItemDefinitionGroup>
  <ItemDefinitionGroup>
    <j />
  </ItemDefinitionGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add a property to an empty project
        /// should add to new property group
        /// </summary>
        [TestMethod]
        public void AddProperty_EmptyProject()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyElement property = project.AddProperty("p", "v1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup>
    <p>v1</p>
  </PropertyGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(property, Helpers.GetFirst(Helpers.GetFirst(project.PropertyGroups).Properties));
        }

        /// <summary>
        /// Add a property to a project with an existing property group
        /// should add to property group
        /// </summary>
        [TestMethod]
        public void AddProperty_ExistingPropertyGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddPropertyGroup();
            ProjectPropertyElement property = project.AddProperty("p", "v1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup>
    <p>v1</p>
  </PropertyGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add a property to a project with an existing property group with condition
        /// should add to new property group
        /// </summary>
        [TestMethod]
        public void AddProperty_ExistingPropertyGroupWithCondition()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
            propertyGroup.Condition = "c";

            ProjectPropertyElement property = project.AddProperty("p", "v1");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup Condition=""c"" />
  <PropertyGroup>
    <p>v1</p>
  </PropertyGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add a property to a project with an existing property with the same name
        /// should modify and return existing property
        /// </summary>
        [TestMethod]
        public void AddProperty_ExistingPropertySameName()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyElement property1 = project.AddProperty("p", "v1");

            ProjectPropertyElement property2 = project.AddProperty("p", "v2");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup>
    <p>v2</p>
  </PropertyGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
            Assert.AreEqual(true, Object.ReferenceEquals(property1, property2));
        }

        /// <summary>
        /// Add a property to a project with an existing property with the same name but a condition;
        /// should add new property
        /// </summary>
        [TestMethod]
        public void AddProperty_ExistingPropertySameNameCondition()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyElement property1 = project.AddProperty("p", "v1");
            property1.Condition = "c";

            ProjectPropertyElement property2 = project.AddProperty("p", "v2");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup>
    <p Condition=""c"">v1</p>
    <p>v2</p>
  </PropertyGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Add a property to a project with an existing property with the same name but a condition;
        /// should add new property
        /// </summary>
        [TestMethod]
        public void AddProperty_ExistingPropertySameNameConditionOnGroup()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyElement property1 = project.AddProperty("p", "v1");
            property1.Parent.Condition = "c";

            ProjectPropertyElement property2 = project.AddProperty("p", "v2");

            string expected = ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <PropertyGroup Condition=""c"">
    <p>v1</p>
  </PropertyGroup>
  <PropertyGroup>
    <p>v2</p>
  </PropertyGroup>
</Project>");

            Helpers.VerifyAssertProjectContent(expected, project);
        }

        /// <summary>
        /// Attempt to add a property with a reserved name
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAddPropertyReservedName()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddProperty("MSBuildToolsPATH", "v");
        }

        /// <summary>
        /// Attempt to add a property with an illegal name
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvalidAddPropertyIllegalName()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddProperty("ItemGroup", "v");
        }

        /// <summary>
        /// Attempt to add a property with an invalid XML name
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidAddPropertyInvalidXmlName()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            project.AddProperty("@#$@#", "v");
        }

        /// <summary>
        /// Too much nesting should not cause stack overflow.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidProjectFileException))]
        public void InvalidChooseOverflow()
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectElementContainer current = project;
            while (true)
            {
                ProjectChooseElement choose = project.CreateChooseElement();
                ProjectWhenElement when = project.CreateWhenElement("c");
                current.AppendChild(choose);
                choose.AppendChild(when);
                current = when;
            }
        }

        /// <summary>
        /// Setting item condition should dirty project
        /// </summary>
        [TestMethod]
        public void Dirtying_ItemCondition()
        {
            XmlReader content = XmlReader.Create(new StringReader(ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"" />
  </ItemGroup>
</Project>")));

            Project project = new Project(content);
            ProjectItem item = Helpers.GetFirst(project.Items);

            item.Xml.Condition = "false";

            Assert.AreEqual(1, Helpers.Count(project.Items));

            project.ReevaluateIfNecessary();

            Assert.AreEqual(0, Helpers.Count(project.Items));
        }

        /// <summary>
        /// Setting metadata condition should dirty project
        /// </summary>
        [TestMethod]
        public void Dirtying_MetadataCondition()
        {
            XmlReader content = XmlReader.Create(new StringReader(ObjectModelHelpers.CleanupFileContents(
@"<Project ToolsVersion=""msbuilddefaulttoolsversion"" xmlns=""msbuildnamespace"">
  <ItemGroup>
    <i Include=""i1"">
      <m>m1</m>
    </i>
  </ItemGroup>
</Project>")));

            Project project = new Project(content);
            ProjectMetadata metadatum = Helpers.GetFirst(project.Items).GetMetadata("m");

            metadatum.Xml.Condition = "false";

            Assert.AreEqual("m1", metadatum.EvaluatedValue);

            project.ReevaluateIfNecessary();
            metadatum = Helpers.GetFirst(project.Items).GetMetadata("m");

            Assert.AreEqual(null, metadatum);
        }

        /// <summary>
        /// Delete all the children of a container, then add them
        /// to a new one, and iterate. Should not go into infinite loop :-)
        /// </summary>
        [TestMethod]
        public void DeleteAllChildren()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            ProjectItemGroupElement group1 = xml.AddItemGroup();
            ProjectItemElement item1 = group1.AddItem("i", "i1");
            ProjectItemElement item2 = group1.AddItem("i", "i2");
            group1.RemoveChild(item1);
            group1.RemoveChild(item2);

            ProjectItemGroupElement group2 = xml.AddItemGroup();
            group2.AppendChild(item1);
            group2.AppendChild(item2);

            List<ProjectElement> allChildren = new List<ProjectElement>(group2.AllChildren);

            Helpers.AssertListsValueEqual(allChildren, new List<ProjectElement> { item1, item2 });
            Assert.AreEqual(0, group1.Count);
        }

        /// <summary>
        /// Same but with Prepend for the 2nd one
        /// </summary>
        [TestMethod]
        public void DeleteAllChildren2()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            ProjectItemGroupElement group1 = xml.AddItemGroup();
            ProjectItemElement item1 = group1.AddItem("i", "i1");
            ProjectItemElement item2 = group1.AddItem("i", "i2");
            group1.RemoveChild(item1);
            group1.RemoveChild(item2);

            ProjectItemGroupElement group2 = xml.AddItemGroup();
            group2.AppendChild(item1);
            group2.PrependChild(item2);

            List<ProjectElement> allChildren = new List<ProjectElement>(group2.AllChildren);

            Helpers.AssertListsValueEqual(allChildren, new List<ProjectElement> { item2, item1 });
            Assert.AreEqual(0, group1.Count);
        }

        /// <summary>
        /// Same but with InsertBefore for the 2nd one
        /// </summary>
        [TestMethod]
        public void DeleteAllChildren3()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            ProjectItemGroupElement group1 = xml.AddItemGroup();
            ProjectItemElement item1 = group1.AddItem("i", "i1");
            ProjectItemElement item2 = group1.AddItem("i", "i2");
            group1.RemoveChild(item1);
            group1.RemoveChild(item2);

            ProjectItemGroupElement group2 = xml.AddItemGroup();
            group2.AppendChild(item1);
            group2.InsertBeforeChild(item2, item1);

            List<ProjectElement> allChildren = new List<ProjectElement>(group2.AllChildren);

            Helpers.AssertListsValueEqual(allChildren, new List<ProjectElement> { item2, item1 });
            Assert.AreEqual(0, group1.Count);
        }

        /// <summary>
        /// Same but with InsertAfter for the 2nd one
        /// </summary>
        [TestMethod]
        public void DeleteAllChildren4()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            ProjectItemGroupElement group1 = xml.AddItemGroup();
            ProjectItemElement item1 = group1.AddItem("i", "i1");
            ProjectItemElement item2 = group1.AddItem("i", "i2");
            group1.RemoveChild(item1);
            group1.RemoveChild(item2);

            ProjectItemGroupElement group2 = xml.AddItemGroup();
            group2.AppendChild(item1);
            group2.InsertAfterChild(item2, item1);

            List<ProjectElement> allChildren = new List<ProjectElement>(group2.AllChildren);

            Helpers.AssertListsValueEqual(allChildren, new List<ProjectElement> { item1, item2 });
            Assert.AreEqual(0, group1.Count);
        }

        /// <summary>
        /// Same but with InsertAfter for the 2nd one
        /// </summary>
        [TestMethod]
        public void DeleteAllChildren5()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            ProjectItemGroupElement group1 = xml.AddItemGroup();
            ProjectItemElement item1 = group1.AddItem("i", "i1");
            ProjectItemElement item2 = group1.AddItem("i", "i2");
            group1.RemoveChild(item1);
            group1.RemoveChild(item2);

            ProjectItemGroupElement group2 = xml.AddItemGroup();
            group2.AppendChild(item1);
            group2.InsertAfterChild(item2, item1);

            List<ProjectElement> allChildren = new List<ProjectElement>(group2.AllChildren);

            Helpers.AssertListsValueEqual(allChildren, new List<ProjectElement> { item1, item2 });
            Assert.AreEqual(0, group1.Count);
        }

        /// <summary>
        /// Move some children
        /// </summary>
        [TestMethod]
        public void DeleteSomeChildren()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            ProjectItemGroupElement group1 = xml.AddItemGroup();
            ProjectItemElement item1 = group1.AddItem("i", "i1");
            ProjectItemElement item2 = group1.AddItem("i", "i2");
            ProjectItemElement item3 = group1.AddItem("i", "i3");
            group1.RemoveChild(item1);
            group1.RemoveChild(item2);

            ProjectItemGroupElement group2 = xml.AddItemGroup();
            group2.AppendChild(item1);
            group2.AppendChild(item2);

            List<ProjectElement> allChildren = new List<ProjectElement>(group2.AllChildren);

            Helpers.AssertListsValueEqual(allChildren, new List<ProjectElement> { item1, item2 });
            Assert.AreEqual(1, group1.Count);
            Assert.AreEqual(true, item3.PreviousSibling == null && item3.NextSibling == null);
            Assert.AreEqual(true, item2.PreviousSibling == item1 && item1.NextSibling == item2);
            Assert.AreEqual(true, item1.PreviousSibling == null && item2.NextSibling == null);
        }

        /// <summary>
        /// Attempt to modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_1()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectImportElement import = project.AddImport("p");
            import.Parent.RemoveAllChildren();
            import.Condition = "c";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_2()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectImportElement import = project.AddImport("p");
            import.Parent.RemoveAllChildren();
            import.Project = "p";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_3()
        {
            ProjectRootElement.Create().CreateImportGroupElement().Condition = "c";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_4()
        {
            var element = ProjectRootElement.Create().AddItemDefinition("i").AddMetadata("m", "M1");
            element.Parent.RemoveAllChildren();
            element.Value = "v1";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_5()
        {
            var element = ProjectRootElement.Create().AddItem("i", "i1").AddMetadata("m", "M1");
            element.Parent.RemoveAllChildren();
            element.Value = "v1";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_5b()
        {
            var element = ProjectRootElement.Create().AddItem("i", "i1");
            element.Parent.RemoveAllChildren();
            element.ItemType = "j";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_6()
        {
            var element = ProjectRootElement.Create().AddItem("i", "i1");
            element.Parent.RemoveAllChildren();
            element.Include = "i2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_7()
        {
            var element = ProjectRootElement.Create().AddProperty("p", "v1");
            element.Parent.RemoveAllChildren();
            element.Value = "v2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_8()
        {
            var element = ProjectRootElement.Create().AddProperty("p", "v1");
            element.Parent.RemoveAllChildren();
            element.Condition = "c";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_9()
        {
            var element = ProjectRootElement.Create().AddUsingTask("n", "af", null);
            element.Parent.RemoveAllChildren();
            element.TaskName = "n2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_10()
        {
            var element = ProjectRootElement.Create().AddUsingTask("n", "af", null);
            element.Parent.RemoveAllChildren();
            element.AssemblyFile = "af2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_11()
        {
            var element = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            element.Parent.RemoveAllChildren();
            element.AssemblyName = "an2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_12()
        {
            var element = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            element.Parent.RemoveAllChildren();
            element.TaskFactory = "tf";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_15()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.RemoveAllChildren();
            element.Name = "n2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_16()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.RemoveAllChildren();
            element.Output = "o2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_17()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.RemoveAllChildren();
            element.Required = "r2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_18()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.RemoveAllChildren();
            element.ParameterType = "pt2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_19()
        {
            var element = ProjectRootElement.Create().AddTarget("t");
            element.Parent.RemoveAllChildren();
            element.Name = "t2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_20()
        {
            var element = ProjectRootElement.Create().AddTarget("t");
            element.Parent.RemoveAllChildren();
            element.Inputs = "i";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_21()
        {
            var element = ProjectRootElement.Create().AddTarget("t");
            element.Parent.RemoveAllChildren();
            element.Outputs = "o";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_22()
        {
            var element = ProjectRootElement.Create().AddTarget("t");
            element.Parent.RemoveAllChildren();
            element.DependsOnTargets = "d";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_23()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt");
            element.Parent.RemoveAllChildren();
            element.SetParameter("p", "v");
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_24()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt");
            element.Parent.RemoveAllChildren();
            element.ContinueOnError = "coe";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_25()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputItem("tp", "i");
            element.Parent.RemoveAllChildren();
            element.TaskParameter = "tp2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_26()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputItem("tp", "i");
            element.Parent.RemoveAllChildren();
            element.ItemType = "tp2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_27()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputProperty("tp", "p");
            element.Parent.RemoveAllChildren();
            element.TaskParameter = "tp2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_28()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputProperty("tp", "p");
            element.Parent.RemoveAllChildren();
            element.PropertyName = "tp2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_29()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddItemGroup().AddItem("i", "i1");
            element.Parent.RemoveAllChildren();
            element.ItemType = "j";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_30()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddItemGroup().AddItem("i", "i1");
            element.Parent.RemoveAllChildren();
            element.Include = "i2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_31()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddItemGroup().AddItem("i", "i1").AddMetadata("m", "m1");
            element.Parent.RemoveAllChildren();
            element.Value = "m2";
        }

        /// <summary>
        /// Legally modify a child that is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedChild_32()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddPropertyGroup().AddProperty("p", "v1");
            element.Parent.RemoveAllChildren();
            element.Value = "v2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_1()
        {
            var element = ProjectRootElement.Create().AddImportGroup().AddImport("p");
            element.Parent.Parent.RemoveAllChildren();
            element.Condition = "c";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_2()
        {
            var element = ProjectRootElement.Create().AddImportGroup().AddImport("p");
            element.Parent.Parent.RemoveAllChildren();
            element.Project = "p";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_3()
        {
            ProjectRootElement.Create().CreateImportGroupElement().Condition = "c";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_4()
        {
            var element = ProjectRootElement.Create().AddItemDefinition("i").AddMetadata("m", "M1");
            element.Parent.Parent.RemoveAllChildren();
            element.Value = "v1";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_5()
        {
            var element = ProjectRootElement.Create().AddItem("i", "i1").AddMetadata("m", "M1");
            element.Parent.Parent.RemoveAllChildren();
            element.Value = "v1";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_5b()
        {
            var element = ProjectRootElement.Create().AddItem("i", "i1");
            element.Parent.Parent.RemoveAllChildren();
            element.ItemType = "j";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_6()
        {
            var element = ProjectRootElement.Create().AddItem("i", "i1");
            element.Parent.Parent.RemoveAllChildren();
            element.Include = "i2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_7()
        {
            var element = ProjectRootElement.Create().AddProperty("p", "v1");
            element.Parent.Parent.RemoveAllChildren();
            element.Value = "v2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_8()
        {
            var element = ProjectRootElement.Create().AddProperty("p", "v1");
            element.Parent.Parent.RemoveAllChildren();
            element.Condition = "c";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_15()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.Parent.RemoveAllChildren();
            element.Name = "n2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_16()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.Parent.RemoveAllChildren();
            element.Output = "o2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_17()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.Parent.RemoveAllChildren();
            element.Required = "r2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_18()
        {
            var usingTask = ProjectRootElement.Create().AddUsingTask("n", null, "an");
            usingTask.TaskFactory = "f";
            var element = usingTask.AddParameterGroup().AddParameter("n", "o", "r", "pt");
            element.Parent.Parent.RemoveAllChildren();
            element.ParameterType = "pt2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_23()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt");
            element.Parent.Parent.RemoveAllChildren();
            element.SetParameter("p", "v");
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_24()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt");
            element.Parent.Parent.RemoveAllChildren();
            element.ContinueOnError = "coe";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_25()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputItem("tp", "i");
            element.Parent.Parent.RemoveAllChildren();
            element.TaskParameter = "tp2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_26()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputItem("tp", "i");
            element.Parent.Parent.RemoveAllChildren();
            element.ItemType = "tp2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_27()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputProperty("tp", "p");
            element.Parent.Parent.RemoveAllChildren();
            element.TaskParameter = "tp2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_28()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddTask("tt").AddOutputProperty("tp", "p");
            element.Parent.Parent.RemoveAllChildren();
            element.PropertyName = "tp2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_29()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddItemGroup().AddItem("i", "i1");
            element.Parent.Parent.RemoveAllChildren();
            element.ItemType = "j";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_30()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddItemGroup().AddItem("i", "i1");
            element.Parent.Parent.RemoveAllChildren();
            element.Include = "i2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_31()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddItemGroup().AddItem("i", "i1").AddMetadata("m", "m1");
            element.Parent.Parent.RemoveAllChildren();
            element.Value = "m2";
        }

        /// <summary>
        /// Legally modify a child whose parent is not parented (should not throw)
        /// </summary>
        [TestMethod]
        public void ModifyUnparentedParentChild_32()
        {
            var element = ProjectRootElement.Create().AddTarget("t").AddPropertyGroup().AddProperty("p", "v1");
            element.Parent.Parent.RemoveAllChildren();
            element.Value = "v2";
        }
    }
}
