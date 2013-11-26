<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="albumtreeview.ascx.cs"
						Inherits="GalleryServerPro.Web.Controls.albumtreeview" %>
<script runat="server" visible="false">
//	var data = [
//		{
//			"data": "Top 1",
//			"metadata": { id: 23 },
//			"state": "closed",
//			"children": [{
//				"data": "Top 1 - Child 1",
//				"state": "closed",
//				"children": [{
//						"data": "Top 1 - Child 1 - Child 1",
//						"attr": {
//							"id": "node-123",
//							"class": "gsp_no_children"
//						}
//					},
//					{
//						"data": "Top 1 - Child 1 - Child 2",
//						"attr": {
//							"id": "node-123",
//							"class": "gsp_no_children"
//						}
//					}]
//			},
//				{
//					"data": "Top 1 - Child 2",
//					"state": "closed",
//					"children": ["Top 1 - Child 2 - Child 1", "Top 1 - Child 2 - Child 2",
//						{
//							"data": "Top 1 - Child 2 - Child 3",
//							"state": "closed",
//							"attr": {
//								"id": "node-456"
//							}
//						}]
//				}]
//		},
//		{
//			"data": "Top 2",
//			"state": "closed",
//			"children": [{
//				"data": "Top 2 - Child 1",
//				"state": "closed",
//				"children": ["Top 2 - Child 1 - Child 1", "Top 2 - Child 1 - Child 2",
//					{
//						"data": "Top 2 - Child 1 - Child 3",
//						"state": "closed",
//						"attr": {
//							"id": "node-789"
//						}
//					}]
//			}]
//		}
//	];

//	data = [ <%=GetTreeData()%> ];

//	$(function() {
//		bindTreeView();
//	});

//	function bindTreeView() {
//		$("#gsp_jstree").jstree('destroy').empty();
//		
//		$("#gsp_jstree").jstree({
//				"themes": {
//					"theme": "<%=TreeViewTheme%>",
//					"dots": false,
//					"icons": false
//				},
//				"json_data": {
//					"data": data,
//					"ajax": {
//						"url": "../gs/gs/handler/gettreeview.ashx",
//						"data": function(n) {
//							// the result is fed to the AJAX request 'data' option
//							document.body.style.cursor = 'wait';
//							//curNode = n;
//							console.log('initiating ajax request');
//							return {
//								"id": n.data("id"),
//								"secaction": "<%=(int) RequiredSecurityPermissions%>",
//								"sc": n.children().is("input:checkbox"), //this.is_checked(n)
//								"navurl": "<%=NavigateUrl%>"
//							};
//						},
//						success: function(data, status) {
//							document.body.style.cursor = 'default';
//						},
//						error: function(response, textStatus, errorThrown) {
//							document.body.style.cursor = 'default';
//							if (textStatus == "error") {
//								alert("Oops! An error occurred while retrieving the treeview data. It has been logged in the gallery's event log.");
//							}
//						}
//					}
//				},
//				"ui": {
//					"selected_parent_close": false
//				},
//				"checkbox": {
//					"real_checkboxes": true,
//					"override_ui": false,
//					"two_state": true
//				},
//				"plugins": ["themes", "ui", "json_data", "checkbox"]
//			})
//			.bind("select_node.jstree", function(e, data) {
//				onNodeSelect(e, data);
//			})
//			.bind("open_node.jstree", function(e, data) {
//				onNodeOpen(e, data);
//			})
//			.bind("deselect_node.jstree", function(e, data) {
//				console.log('deselect_node: ' + data.inst.get_selected());
//			})
//			.bind("check_node.jstree", function(e, data) {
//				//debugger;
//				console.log('check_node: ' + data.inst.get_selected());
//			})
//			.bind("get_json.jstree", function(e, data) {
//				console.log('get_json: ' + data.inst.get_selected());
//			})
//			.bind("change_state.jstree", function(e, data) {
//				onChangeState(e, data);
//				// Don't let more than one be checked
//				//if(data.inst.get_checked().length > 1){ 
//				//	data.inst.uncheck_node(data.rslt[0]); 
//				//} 
//			})
//			.bind("loaded.jstree", function(e, data) {
//				console.log('loaded');
//				var tv = $(this);
//				$.each("<%=NodesToCheckIdJson%>", function(i, id) {
//					tv.jstree("select_node", "#" + id);
//				});
//			});
//	}
//	
//	function onChangeState(e, data) {
//		var topCheckedNodes = $(data.inst.get_checked()).not(function(idx) {
//			var id = $(this).parents('.jstree-checked');
//			return (id.length > 0);
//		});
//		var albumIds = topCheckedNodes.map(function(){ return $(this).data("id"); }).get().join();
//		$('#<%=hdnCheckedAlbumIds.ClientID%>').val(albumIds);
//		console.log('Album IDs: ' + albumIds);
//		console.log('There are ' + topCheckedNodes.length + ' top level checked nodes.');
//		console.log('change_state: ' + data.inst.get_selected());
//	}

//	function onNodeOpen(e, data) {
//		console.log('open_node: ' + data.args[0]);
//		var tv = data.inst;
//		var n = data.args[0];
//		if (tv.is_checked(n)) {
//			checkAll(tv, n);
//		}
//	}

//	function onNodeSelect(e, data) {
//		console.log('select_node: ' + data.inst.get_selected());
//		//debugger;
//		//data.inst.uncheck_all();
//		var tv = data.inst;
//		var n = tv.get_selected();
//		if (tv.is_checked(n)) {
//			uncheckAll(tv, n, true);
//		} else {
//			checkAll(tv, n);
//		}
//	}

//	function checkAll(tv, n) {
//		tv.check_node(n);
//		if (tv.is_open(n)) {
//			$.each(tv._get_children(n), function(i, childNode) {
//				checkAll(tv, childNode);
//			});
//		}
//	}

//	function uncheckAll(tv, n, navigateUp) {
//		if (n == null || n == -1)
//			return;
//		
//		tv.uncheck_node(n);
//		if (navigateUp) {
//			uncheckAll(tv, tv._get_parent(n), navigateUp);
//		} else {
//			// Drill down, unchecking along the way
//			$.each(tv._get_children(n), function(i, childNode) {
//				uncheckAll(tv, childNode, navigateUp);
//			});
//		}
//	}
//		
//	function bindTreeViewSimple() {
//		$("#g_ctl00_tvContainer").jstree({
//				"themes": {
//					"theme": "gsp"
//				},
//				"json_data": {
//					"data": data
//				},
//				"checkbox": {
//					"two_state": true
//				},
//				"plugins": ["themes", "ui", "json_data", "checkbox"]
//			});
//	}

</script>
<div id="<%= TreeViewClientId %>">
</div>
<asp:HiddenField ID="hdnCheckedAlbumIds" runat="server" />
