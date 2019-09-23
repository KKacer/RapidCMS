﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.WindowsAzure.Storage;
using RapidCMS.Common.Data;
using RapidCMS.Common.Enums;
using RapidCMS.Common.Extensions;
using RapidCMS.Common.Models.Config;
using TestLibrary;
using TestLibrary.Authorization;
using TestLibrary.Data;
using TestLibrary.DataProvider;
using TestLibrary.Entities;
using TestLibrary.Enums;
using TestLibrary.Repositories;
using TestServer.ActionHandlers;
using TestServer.Components.CustomButtons;
using TestServer.Components.CustomEditors;
using TestServer.Components.CustomLogin;
using TestServer.Components.CustomSections;
using Blazor.FileReader;
using TestServer.Components.CustomDashboard;
using TestLibrary.DataViewBuilders;
using TestServer.Components.CustomSidePane;

namespace TestServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            #region Authorization

            // ***********************************************
            // For more info on:
            // Microsoft.AspNetCore.Authentication.AzureAD.UI
            // see:
            // https://bit.ly/2Fv6Zxp
            // This creates a 'virtual' controller 
            // called 'Account' in an Area called 'AzureAd' that allows the
            // 'AzureAd/Account/SignIn' and 'AzureAd/Account/SignOut'
            // links to work
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));

            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential
            //    // cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => true;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});

            // This configures the 'middleware' pipeline
            // This is where code to determine what happens
            // when a person logs in is configured and processed
            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Instead of using the default validation 
                    // (validating against a single issuer value, as we do in
                    // line of business apps), we inject our own multitenant validation logic
                    ValidateIssuer = false,
                    // If the app is meant to be accessed by entire organizations, 
                    // add your issuer validation logic here.
                    //IssuerValidator = (issuer, securityToken, validationParameters) => {
                    //    if (myIssuerValidationLogic(issuer)) return issuer;
                    //}
                };
                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        // If your authentication logic is based on users 
                        // then add your logic here
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect("/Error");
                        context.HandleResponse(); // Suppress the exception
                        return Task.CompletedTask;
                    },
                    OnSignedOutCallbackRedirect = context =>
                    {
                        // This is called when a user logs out
                        // redirect them back to the main page
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    },
                    // If your application needs to do authenticate single users, 
                    // add your user validation below.
                    //OnTokenValidated = context =>
                    //{
                    //    return myUserValidationLogic(context.Ticket.Principal);
                    //}
                };
            });

            #endregion

            services.AddDbContext<TestDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SqlConnectionString"));
            });

            services.AddFileReaderService(options => options.InitializeOnFirstCall = true);

            services.AddScoped<RepositoryA>();
            services.AddScoped<RepositoryB>();
            services.AddScoped<RepositoryC>();
            services.AddScoped<RepositoryD>();
            services.AddScoped<RepositoryE>();
            services.AddScoped<RepositoryF>();
            services.AddScoped<VariantRepository>();
            services.AddScoped<ValidationRepository>();

            services.AddScoped<RelationRepository>();

            services.AddScoped<CountryRepository>();
            services.AddScoped<PersonRepository>();

            services.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
            services.AddScoped<AzureTableStorageRepository>();

            services.AddTransient<CreateButtonActionHandler>();
            services.AddTransient<TogglePropertyButtonActionHandler>();

            services.AddTransient<DummyDataProvider>();

            services.AddScoped<CountryDataViewBuilder>();

            services.AddSingleton<IAuthorizationHandler, CountryEntityAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, PersonEntityAuthorizationHandler>();

            services.AddRapidCMS(config =>
            {
                config.AllowAnonymousUser();

                config.SetCustomLogin(typeof(LoginControl));

                config.SetSiteName("Test Client");

                config.AddDashboardSection(typeof(CustomDashboard));
                config.AddDashboardSection("person-collection", edit: false);

                config.AddCollection<ValidationEntity>("validation-collection", "Validation entities", collection =>
                {
                    collection
                        .SetRepository<ValidationRepository>()
                        .SetTreeView(e => e.Name)
                        .SetListView(list =>
                        {
                            list.SetSearchBarVisibility(false);

                            list.AddDefaultButton(DefaultButtonType.New);
                            list.AddPaneButton(typeof(SidePaneTest), "Create new elements", "add", CrudType.Create);
                            list.AddRow(pane =>
                            {
                                pane.AddField(p => p.Name)
                                    .VisibleWhen(f => f.Accept);
                                pane.AddDefaultButton(DefaultButtonType.View);
                                pane.AddDefaultButton(DefaultButtonType.Edit);
                                pane.AddPaneButton(typeof(SidePaneTest), "Delete element", "trash", CrudType.Delete);
                            });
                        })
                        //.SetListEditor(ListType.Table, listEditor =>
                        //{
                        //    listEditor.SetSearchBarVisibility(false);

                        //    listEditor.AddDefaultButton(DefaultButtonType.New);
                        //    listEditor.AddDefaultButton(DefaultButtonType.Return);

                        //    listEditor.AddSection(editor =>
                        //    {
                        //        editor.AddDefaultButton(DefaultButtonType.View);
                        //        editor.AddDefaultButton(DefaultButtonType.SaveNew);
                        //        editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                        //        editor.AddDefaultButton(DefaultButtonType.Delete);

                        //        //editor.AddField(f => f.Name);
                        //        //editor.AddField(f => f.NotRequired);
                        //        //editor.AddField(f => f.Range)
                        //        //    .SetName("Range Setting");

                        //        editor.AddField(f => f.CountryId)
                        //            .SetType(EditorType.Select)
                        //            .SetCollectionRelation<CountryEntity>("country-collection", relation =>
                        //            {
                        //                relation
                        //                    .SetElementIdProperty(x => x._Id)
                        //                    .SetElementDisplayProperties(x => x._Id.ToString(), x => x.Name);

                        //            });
                        //    });
                        //})
                        .SetNodeView(view =>
                        {
                            view.AddSection(pane =>
                            {
                                pane.SetLabel("THIS IS A LABEL");

                                pane.AddDefaultButton(DefaultButtonType.Delete);

                                pane.AddField(f => f.Name);
                                pane.AddField(f => f.Dummy)
                                    .VisibleWhen(f => f.Name == "Country123");

                                pane.AddSubCollectionList<CountryEntity>("country-collection");
                            });
                        })
                        .SetNodeEditor(editor =>
                        {
                            editor.AddDefaultButton(DefaultButtonType.SaveNew);
                            editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                            editor.AddDefaultButton(DefaultButtonType.Delete);
                            editor.AddSection(pane =>
                            {
                                pane.AddCustomButton<TogglePropertyButtonActionHandler>(typeof(DoItButton));

                                pane.AddField(f => f.Name);
                                pane.AddField(f => f.Date);
                                pane.AddField(f => f.Dummy).SetType(typeof(UploadEditor));
                                pane.AddField(f => f.NotRequired);
                                pane.AddField(f => f.Range)
                                    .SetName("Range Setting");
                                pane.AddField(f => f.Accept)
                                    .SetName("Accept this");
                                pane.AddField(f => f.Textarea)
                                    .VisibleWhen(f => f.Accept)
                                    .SetType(EditorType.TextArea);
                            });
                            editor.AddSection(pane =>
                            {
                                pane.SetLabel("THIS IS A LABEL");

                                pane.AddDefaultButton(DefaultButtonType.Delete);

                                pane.VisibleWhen(x => x.Accept);

                                pane.AddField(f => f.Enum)
                                    .SetType(EditorType.Select)
                                    .SetDataCollection<EnumDataProvider<TestEnum>>();

                                pane.AddField(f => f.CountryId)
                                    .SetType(EditorType.Select)
                                    .SetCollectionRelation<CountryEntity>("country-collection", relation =>
                                    {
                                        relation
                                            .SetElementIdProperty(x => x._Id)
                                            .SetElementDisplayProperties(x => x._Id.ToString(), x => x.Name)
                                            .SetRepositoryParentIdProperty(x => x.Id);

                                    });

                                pane.AddSubCollectionList<CountryEntity>("country-collection");
                            });
                        })
                        .AddCollection<ValidationEntity>("validation-collection-nested", "Validation entities", subCollection =>
                        {
                            subCollection
                                .SetRepository<ValidationRepository>()
                                .SetTreeView(EntityVisibilty.Visible, CollectionRootVisibility.Hidden, e => e.Name)
                                .SetListView(list =>
                                {
                                    list.SetSearchBarVisibility(false);

                                    list.AddDefaultButton(DefaultButtonType.New);
                                    list.AddPaneButton(typeof(SidePaneTest), "Create new elements", "plus", CrudType.Create);
                                    list.AddRow(pane =>
                                    {
                                        pane.AddField(p => p.Name)
                                            .VisibleWhen(f => f.Accept);
                                        pane.AddDefaultButton(DefaultButtonType.View);
                                        pane.AddDefaultButton(DefaultButtonType.Edit);
                                        pane.AddPaneButton(typeof(SidePaneTest), "Delete element", "trash", CrudType.Delete);
                                    });
                                })
                                .SetNodeEditor(editor =>
                                {
                                    editor.AddDefaultButton(DefaultButtonType.SaveNew);
                                    editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                                    editor.AddDefaultButton(DefaultButtonType.Delete);
                                    editor.AddSection(pane =>
                                    {
                                        pane.AddCustomButton<TogglePropertyButtonActionHandler>(typeof(DoItButton));

                                        pane.AddField(f => f.Name);
                                        pane.AddField(f => f.Date);
                                        pane.AddField(f => f.Dummy).SetType(typeof(UploadEditor));
                                        pane.AddField(f => f.NotRequired);
                                        pane.AddField(f => f.Range)
                                            .SetName("Range Setting");
                                        pane.AddField(f => f.Accept)
                                            .SetName("Accept this");
                                        pane.AddField(f => f.Textarea)
                                            .VisibleWhen(f => f.Accept)
                                            .SetType(EditorType.TextArea);
                                    });
                                    editor.AddSection(pane =>
                                    {
                                        pane.SetLabel("THIS IS A LABEL");

                                        pane.AddDefaultButton(DefaultButtonType.Delete);

                                        pane.VisibleWhen(x => x.Accept);

                                        pane.AddField(f => f.Enum)
                                            .SetType(EditorType.Select)
                                            .SetDataCollection<EnumDataProvider<TestEnum>>();

                                        pane.AddField(f => f.CountryId)
                                            .SetType(EditorType.Select)
                                            .SetCollectionRelation<CountryEntity>("country-collection", relation =>
                                            {
                                                relation
                                                    .SetElementIdProperty(x => x._Id)
                                                    .SetElementDisplayProperties(x => x._Id.ToString(), x => x.Name)
                                                    .SetRepositoryParentIdProperty(x => x.Id);

                                            });

                                        pane.AddSubCollectionList<CountryEntity>("country-collection");
                                    });
                                })
                                .AddSelfAsRecursiveCollection();
                        });

                });

                config.AddCollection<CountryEntity>("related-country-collection", "Countries", collection =>
                {
                    collection
                        .SetRepository<CountryRepository>()
                        .AddDataView("All", x => true)
                        .AddDataView("Countries with 1234 in their name", x => x.Name.Contains("1234"))
                        .SetListView(list =>
                        {
                            list.SetPageSize(3);

                            list.AddDefaultButton(DefaultButtonType.Return);
                            list.AddRow(pane =>
                            {
                                pane.AddField(p => p.Name);
                                pane.AddDefaultButton(DefaultButtonType.Pick);
                            });
                        })
                        .SetListEditor(ListType.Table, list =>
                        {
                            list.SetPageSize(3);

                            list.AddDefaultButton(DefaultButtonType.New);
                            list.AddDefaultButton(DefaultButtonType.Add);

                            list.AddDefaultButton(DefaultButtonType.SaveExisting, "Update all");
                            list.AddSection(pane =>
                            {
                                pane.AddField(p => p.Name);
                                pane.AddDefaultButton(DefaultButtonType.SaveNew);

                                pane.AddDefaultButton(DefaultButtonType.Delete);
                                pane.AddDefaultButton(DefaultButtonType.Remove);
                            });
                        });
                });

                config.AddCollection<CountryEntity>("country-collection", "Countries", collection =>
                {
                    collection
                        .SetRepository<CountryRepository>()
                        .SetTreeView(entity => entity.Name)
                        //.SetDataViewBuilder<CountryDataViewBuilder>()
                        .SetListView(list =>
                        {
                            list.AddDefaultButton(DefaultButtonType.New);
                            list.AddRow(pane =>
                            {
                                pane.AddField(p => p.Name);
                                pane.AddDefaultButton(DefaultButtonType.View);
                                pane.AddDefaultButton(DefaultButtonType.Edit);
                            });
                            //list.AddRow(pane =>
                            //{
                            //    pane.AddProperty(p => p._Id.ToString());
                            //});
                            // list.AddRow(typeof(DashboardSection), config => { });
                        })
                        .SetListEditor(ListType.Table, list =>
                        {
                            list.AddDefaultButton(DefaultButtonType.New);
                            list.AddDefaultButton(DefaultButtonType.SaveExisting, "Update all");

                            list.AddSection(pane =>
                            {
                                pane.AddField(p => p.Name);
                                //pane.AddDefaultButton(DefaultButtonType.SaveExisting);
                                pane.AddDefaultButton(DefaultButtonType.SaveNew);
                            });
                            // list.AddSection(typeof(BlockSection), config => { });
                        })
                        .SetNodeView(editor =>
                        {
                            editor.AddDefaultButton(DefaultButtonType.SaveNew);
                            editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                            editor.AddDefaultButton(DefaultButtonType.Delete);
                            editor.AddSection(pane =>
                            {
                                pane.AddField(f => f.Name);
                            });
                            // editor.AddSection(typeof(BlockSection), config => { });
                        })
                        .SetNodeEditor(editor =>
                        {
                            editor.AddDefaultButton(DefaultButtonType.SaveNew);
                            editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                            editor.AddDefaultButton(DefaultButtonType.Delete);
                            editor.AddSection(pane =>
                            {
                                pane.AddField(f => f.Name);
                            });
                            // editor.AddSection(typeof(BlockSection), config => { });
                        });
                });

                config.AddCollection<PersonEntity>("person-collection", "Persons", collection =>
                {
                    collection
                        .SetRepository<PersonRepository>()
                        .SetTreeView(entity => entity.Name)
                        //.SetListEditor(ListEditorType.Block, editor =>
                        //{
                        //    editor.AddDefaultButton(DefaultButtonType.New);
                        //    editor.AddEditor(pane =>
                        //    {
                        //        pane.AddField(p => p.Name);
                        //        pane.AddField(f => f.Countries.Select(x => x.CountryId))
                        //            .SetName("Countries")
                        //            .SetType(EditorType.MultiSelect)
                        //            .SetCollectionRelation<CountryEntity>("country-collection", relation =>
                        //            {
                        //                relation
                        //                    .SetElementIdProperty(x => x._Id)
                        //                    .SetElementDisplayProperties(x => x._Id.ToString(), x => x.Name);

                        //                relation
                        //                    .ValidateRelation((person, related) =>
                        //                    {
                        //                        if (!related.Count().In(2, 3))
                        //                        {
                        //                            return new[] { "Person must have 2 or 3 countries." };
                        //                        }

                        //                        return default;
                        //                    });
                        //            });
                        //        pane.AddDefaultButton(DefaultButtonType.View);
                        //        pane.AddDefaultButton(DefaultButtonType.Edit);
                        //        pane.AddDefaultButton(DefaultButtonType.Delete);
                        //        pane.AddDefaultButton(DefaultButtonType.SaveNew);
                        //        pane.AddDefaultButton(DefaultButtonType.SaveExisting);
                        //    });
                        //})
                        .SetListView(view =>
                        {
                            view.SetPageSize(2);

                            view.AddDefaultButton(DefaultButtonType.New);
                            view.AddRow(pane =>
                            {
                                pane.AddDefaultButton(DefaultButtonType.New);
                                pane.AddField(p => p.Name);
                                
                                pane.AddDefaultButton(DefaultButtonType.View);
                                pane.AddDefaultButton(DefaultButtonType.Edit);
                                pane.AddDefaultButton(DefaultButtonType.Delete);
                            });
                        })
                        .SetNodeView(editor =>
                        {
                            editor.AddDefaultButton(DefaultButtonType.SaveNew);
                            editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                            editor.AddDefaultButton(DefaultButtonType.Delete);
                            editor.AddSection(pane =>
                            {
                                pane.AddField(f => f.Name);

                                pane.AddRelatedCollectionList<CountryEntity>("related-country-collection");
                            });
                        })
                        .SetNodeEditor(editor =>
                        {
                            editor.AddDefaultButton(DefaultButtonType.SaveNew);
                            editor.AddDefaultButton(DefaultButtonType.SaveExisting);
                            editor.AddDefaultButton(DefaultButtonType.Delete);
                            editor.AddSection(pane =>
                            {
                                pane.AddField(f => f.Name);
                            });
                            editor.AddSection(pane =>
                            {
                                pane.AddField(f => f.Countries)
                                    .SetName("Countries")
                                    .SetType(EditorType.MultiSelect)
                                    .SetCollectionRelation<CountryEntity, int?>(
                                        countries => countries.Select(x => x.CountryId),
                                        "related-country-collection", 
                                        relation =>
                                        {
                                            relation
                                                .SetElementIdProperty(x => x._Id)
                                                .SetElementDisplayProperties(x => x._Id.ToString(), x => x.Name);
                                        });
                            });
                        });
                });

                config.AddCollection<PersonEntity>("person-nested-editor-collection", "Persons (list)", collection =>
                {
                    collection
                        .SetRepository<PersonRepository>()
                        .SetTreeView(EntityVisibilty.Hidden)
                        .SetListEditor(ListType.Block, editor =>
                        {
                            editor.AddDefaultButton(DefaultButtonType.New);

                            editor.AddSection(pane =>
                            {
                                pane.AddDefaultButton(DefaultButtonType.Delete);
                                pane.AddDefaultButton(DefaultButtonType.SaveNew);
                                pane.AddDefaultButton(DefaultButtonType.SaveExisting);

                                pane.AddField(p => p.Name);
                            
                                pane.AddRelatedCollectionList<CountryEntity>("related-country-collection");

                            });
                        });
                });

                //config.AddCollection<RelationEntity>("collection-11", "Azure Table Storage Collecation with relations", collection =>
                //{
                //    collection
                //        .SetRepository<RelationRepository>()
                //        .SetTreeView(EntityVisibilty.Visible, entity => entity.Name)
                //        .SetListView(config =>
                //        {
                //            config.AddDefaultButton(DefaultButtonType.New);

                //            config.AddRow(listPaneConfig =>
                //            {
                //                listPaneConfig.AddProperty(x => x.Id);
                //                listPaneConfig.AddProperty(x => x.Name);
                //                listPaneConfig.AddProperty(x => x.AzureTableStorageEntityId)
                //                    .SetName("Entity");
                //                listPaneConfig.AddProperty(x => string.Join(", ", x.AzureTableStorageEntityIds ?? Enumerable.Empty<string>()))
                //                    .SetName("Entities");

                //                listPaneConfig.AddDefaultButton(DefaultButtonType.Edit, isPrimary: true);
                //                listPaneConfig.AddDefaultButton(DefaultButtonType.Delete);
                //            });
                //        })
                //        .SetNodeEditor(config =>
                //        {
                //            config.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                //            config.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                //            config.AddDefaultButton(DefaultButtonType.Delete);

                //            config.AddSection(editorPaneConfig =>
                //            {
                //                editorPaneConfig.AddField(x => x.Name);

                //                editorPaneConfig.AddField(x => x.Location)
                //                    .SetType(EditorType.Dropdown)
                //                    .SetDataCollection<DummyDataProvider>();

                //                editorPaneConfig.AddField(x => x.AzureTableStorageEntityId)
                //                    .SetName("Entity")
                //                    .SetType(EditorType.Select)
                //                    .SetCollectionRelation<AzureTableStorageEntity>("collection-10", relation =>
                //                    {
                //                        relation
                //                            .SetElementIdProperty(x => x.Id)
                //                            .SetElementDisplayProperties(x => x.Description);
                //                    });

                //                editorPaneConfig.AddField(x => x.AzureTableStorageEntityIds)
                //                    .SetName("Entities")
                //                    .SetType(EditorType.MultiSelect)
                //                    .SetCollectionRelation<AzureTableStorageEntity>("collection-10", relation =>
                //                    {
                //                        relation
                //                            .SetElementIdProperty(x => x.Id)
                //                            .SetElementDisplayProperty(x => x.Description);
                //                    });
                //            });
                //        });
                //});

                config.AddCollection<AzureTableStorageEntity>("collection-10", "Azure Table Storage Collection", collection =>
                {
                    collection
                        .SetRepository<AzureTableStorageRepository>()
                        .SetTreeView(EntityVisibilty.Visible, entity => entity.Title)
                        .SetListView(AzureTableStorageListView)
                        .SetNodeEditor(AzureTableStorageEditor)

                        .AddCollection<AzureTableStorageEntity>("collection-10-a", "Sub collection", subCollection =>
                        {
                            subCollection
                                .SetRepository<AzureTableStorageRepository>()
                                .SetTreeView(EntityVisibilty.Visible, entity => entity.Title)
                                .SetListView(AzureTableStorageListView)
                                .SetNodeEditor(AzureTableStorageEditor);
                        });
                });

                //config.AddCollection<TestEntity>("collection-6", "Variant collection as blocks", collection =>
                //{
                //    collection
                //        .SetRepository<VariantRepository>()
                //        .SetTreeView(EntityVisibilty.Hidden, entity => entity.Name)
                //        .AddEntityVariant<TestEntityVariantA>("Variant A", "align-left")
                //        .AddEntityVariant<TestEntityVariantB>("Variant B", "align-center")
                //        .AddEntityVariant<TestEntityVariantC>("Variant C", "align-right")
                //        .SetListEditor(ListEditorType.Table, EmptyVariantColumnVisibility.Collapse, listNodeEditorWithPolymorphism)
                //        .SetNodeEditor(nodeEditorWithPolymorphism);
                //});

                //config.AddCollection<TestEntity>("collection-5", "Collections with variant sub collection", collection =>
                //{
                //    collection
                //        .SetRepository<RepositoryF>()
                //        .SetTreeView(EntityVisibilty.Visible, entity => entity.Name)
                //        .SetListView(listView)
                //        .SetNodeEditor(nodeEditorWithPolymorphicSubCollection)
                //        .AddCollection<TestEntity>("sub-collection-3", "Sub Collection 3", subCollection =>
                //        {
                //            subCollection
                //                .SetRepository<VariantRepository>()
                //                .SetTreeView(EntityVisibilty.Visible, CollectionRootVisibility.Hidden, entity => entity.Name)
                //                .AddEntityVariant<TestEntityVariantA>("Variant A", "align-left")
                //                .AddEntityVariant<TestEntityVariantB>("Variant B", "align-center")
                //                .AddEntityVariant<TestEntityVariantC>("Variant C", "align-right")
                //                .SetListView(listViewWithPolymorphism)
                //                .SetListEditor(ListEditorType.Table, listNodeEditorWithPolymorphism)
                //                .SetNodeEditor(nodeEditorWithPolymorphism);
                //        });
                //});

                //config.AddCollection<TestEntity>("collection-4", "Collection with sub collections", collection =>
                //{
                //    collection
                //        .SetRepository<RepositoryA>()
                //        .SetTreeView(entity => entity.Name)
                //        .SetListView(listView)
                //        .SetNodeEditor(nodeEditorWithSubCollection)
                //        .AddCollection<TestEntity>("sub-collection-1", "Sub Collection 1", subCollection =>
                //        {
                //            subCollection
                //                .SetRepository<RepositoryB>()
                //                //.SetTreeView(EntityVisibilty.Hidden, CollectionRootVisibility.Hidden, entity => entity.Name)
                //                .SetListView(listView)
                //                .SetListEditor(ListEditorType.Table, subListNodeEditor)
                //                .SetNodeEditor(nodeEditor);
                //        })
                //        .AddCollection<TestEntity>("sub-collection-2", "Sub Collection 2", subCollection =>
                //        {
                //            subCollection
                //                .SetRepository<RepositoryC>()
                //                //.SetTreeView(EntityVisibilty.Hidden, CollectionRootVisibility.Hidden, entity => entity.Name)
                //                .SetListEditor(ListEditorType.Block, subListNodeEditor)
                //                .SetNodeEditor(nodeEditor);
                //        });
                //});

                //config.AddCollection<TestEntity>("collection-3", "Variant collection", collection =>
                //{
                //    collection
                //        .SetRepository<VariantRepository>()
                //        .SetTreeView(EntityVisibilty.Visible, entity => entity.Name)
                //        .AddEntityVariant<TestEntityVariantA>("Variant A", "align-left")
                //        .AddEntityVariant<TestEntityVariantB>("Variant B", "align-center")
                //        .AddEntityVariant<TestEntityVariantC>("Variant C", "align-right")
                //        .SetListView(listView)
                //        .SetNodeEditor(nodeEditorWithPolymorphism);
                //});

                //config.AddCollection<TestEntity>("collection-2", "List editor collection", collection =>
                //{
                //    collection
                //        .SetRepository<RepositoryD>()
                //        .SetTreeView(EntityVisibilty.Hidden, entity => entity.Name)
                //        .SetListEditor(ListEditorType.Table, listNodeEditor)
                //        .SetNodeEditor(nodeEditor);
                //});

                //config.AddCollection<TestEntity>("collection-1", "Simple collection", collection =>
                //{
                //    collection
                //        .SetRepository<RepositoryE>()
                //        .SetTreeView(EntityVisibilty.Visible, entity => entity.Name)
                //        .SetListView(listView)
                //        .SetNodeEditor(nodeEditor);
                //});
            });

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            }).AddNewtonsoftJson();

            services.AddRazorPages();
            services.AddServerSideBlazor();

            #region Editors

            void listView(ListViewConfig<TestEntity> listViewConfig)
            {
                listViewConfig
                    .AddDefaultButton(DefaultButtonType.New, "New", isPrimary: true)
                    .AddRow(pane =>
                    {
                        pane.AddField(x => x._Id.ToString()).SetName("Id");
                        pane.AddField(x => x.Name).SetDescription("This is a name");
                        pane.AddField(x => x.Description).SetDescription("This is a description");
                        pane.AddDefaultButton(DefaultButtonType.View, string.Empty);
                        pane.AddDefaultButton(DefaultButtonType.Edit, string.Empty);

                    });
            }

            void listViewWithPolymorphism(ListViewConfig<TestEntity> listViewConfig)
            {
                listViewConfig
                    .AddDefaultButton(DefaultButtonType.New, "New", isPrimary: true)
                    .AddRow(pane =>
                    {
                        pane.AddField(x => x._Id.ToString()).SetName("Id");
                        pane.AddField(x => x.Name).SetDescription("This is a name");
                        pane.AddField(x => x.Description).SetDescription("This is a description");
                        pane.AddDefaultButton(DefaultButtonType.View, string.Empty);
                        pane.AddDefaultButton(DefaultButtonType.Edit, string.Empty);

                    });
            }

            void nodeEditor(NodeEditorConfig<TestEntity> nodeEditorConfig)
            {
                nodeEditorConfig
                    .AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.Delete)
                    .AddSection(pane =>
                    {
                        pane.SetLabel("General");

                        pane.AddField(x => x._Id)
                            .SetReadonly(true);

                        pane.AddField(x => x.Name)
                            .SetDescription("This is a name");

                        pane.AddField(x => x.Description)
                            .SetDescription("This is a description")
                            .SetType(EditorType.TextArea);

                        pane.AddField(x => x.Number)
                            .SetDescription("This is a number")
                            .SetType(EditorType.Numeric);
                    });
            }

            void listNodeEditor(ListEditorConfig<TestEntity> listEditorConfig)
            {
                listEditorConfig.AddDefaultButton(DefaultButtonType.New);
                listEditorConfig.AddSection(editor =>
                {
                    editor.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.View);
                    editor.AddDefaultButton(DefaultButtonType.Edit);
                    editor.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.Delete);

                    editor.AddField(x => x._Id)
                        .SetDescription("This should be readonly")
                        .SetReadonly();

                    editor.AddField(x => x.Name)
                        .SetDescription("This is a name");

                    editor.AddField(x => x.Description)
                        .SetDescription("This is a description")
                        .SetType(EditorType.TextArea);

                    editor.AddField(x => x.Number)
                        .SetDescription("This is a number")
                        .SetType(EditorType.Numeric);
                });
            }

            void subListNodeEditor(ListEditorConfig<TestEntity> listEditorConfig)
            {
                listEditorConfig.AddDefaultButton(DefaultButtonType.New);
                listEditorConfig.AddSection(editor =>
                {
                    editor.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.View);
                    editor.AddDefaultButton(DefaultButtonType.Edit);
                    editor.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.Delete);

                    editor.AddField(x => x._Id)
                        .SetDescription("This should be readonly")
                        .SetReadonly();

                    editor.AddField(x => x.Name)
                        .SetDescription("This is a name");

                    editor.AddField(x => x.Description)
                        .SetDescription("This is a description")
                        .SetType(EditorType.TextArea);

                    editor.AddField(x => x.Number)
                        .SetDescription("This is a number")
                        .SetType(EditorType.Numeric);
                });
            }

            void nodeEditorWithSubCollection(NodeEditorConfig<TestEntity> nodeEditorConfig)
            {
                nodeEditorConfig
                    .AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.Delete)

                    .AddSection(pane =>
                    {
                        pane.AddField(x => x._Id)
                            .SetReadonly(true);

                        pane.AddField(x => x.Name)
                            .SetDescription("This is a name");

                        pane.AddField(x => x.Description)
                            .SetDescription("This is a description")
                            .SetType(EditorType.TextArea);

                        pane.AddField(x => x.Number)
                            .SetDescription("This is a number")
                            .SetType(EditorType.Numeric);
                    })

                    .AddSection(pane =>
                    {
                        pane.AddSubCollectionList<TestEntity>("sub-collection-1");
                    })

                    .AddSection(pane =>
                    {
                        pane.AddSubCollectionList<TestEntity>("sub-collection-2");
                    });
            }

            void nodeEditorWithPolymorphicSubCollection(NodeEditorConfig<TestEntity> nodeEditorConfig)
            {
                nodeEditorConfig
                    .AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.Delete)

                    .AddSection(pane =>
                    {
                        pane.AddField(x => x._Id)
                            .SetReadonly(true);

                        pane.AddField(x => x.Name)
                            .SetDescription("This is a name");

                        pane.AddField(x => x.Description)
                            .SetDescription("This is a description")
                            .SetType(EditorType.TextArea);

                        pane.AddField(x => x.Number)
                            .SetDescription("This is a number")
                            .SetType(EditorType.Numeric);
                    })

                    .AddSection(pane =>
                    {
                        pane.AddSubCollectionList<TestEntity>("sub-collection-3");
                    });
            }

            void nodeEditorWithPolymorphism(NodeEditorConfig<TestEntity> nodeEditorConfig)
            {
                nodeEditorConfig
                    .AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true)
                    .AddDefaultButton(DefaultButtonType.Delete)

                    .AddSection(pane =>
                    {
                        pane.AddField(x => x._Id)
                            .SetReadonly(true);

                        pane.AddField(x => x.Name)
                            .SetDescription("This is a name");

                        pane.AddField(x => x.Description)
                            .SetDescription("This is a description")
                            .SetType(EditorType.TextArea);

                        pane.AddField(x => x.Number)
                            .SetDescription("This is a number")
                            .SetType(EditorType.Numeric);
                    })

                    .AddSection<TestEntityVariantA>(pane =>
                    {
                        pane.AddField(x => x.Title)
                            .SetDescription("This is a title");
                    })

                    .AddSection<TestEntityVariantB>(pane =>
                    {
                        pane.AddField(x => x.Image)
                            .SetDescription("This is an image");
                    })

                    .AddSection<TestEntityVariantC>(pane =>
                    {
                        pane.AddField(x => x.Quote)
                            .SetDescription("This is a quote");
                    });
            }

            void listNodeEditorWithPolymorphism(ListEditorConfig<TestEntity> listEditorConfig)
            {
                listEditorConfig.AddDefaultButton(DefaultButtonType.New, isPrimary: true);
                listEditorConfig.AddSection<TestEntityVariantA>(editor =>
                {
                    editor.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.View);
                    editor.AddDefaultButton(DefaultButtonType.Edit);
                    editor.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.Delete);

                    editor.AddField(x => x._Id)
                        .SetDescription("This should be readonly")
                        .SetReadonly();

                    editor.AddField(x => x.Name)
                        .SetDescription("This is a name");

                    editor.AddField(x => x.Description)
                        .SetDescription("This is a description")
                        .SetType(EditorType.TextArea);

                    editor.AddField(x => x.Number)
                        .SetDescription("This is a number")
                        .SetType(EditorType.Numeric);

                    editor.AddField(x => x.Title)
                        .SetDescription("This is a title");
                });

                listEditorConfig.AddSection<TestEntityVariantB>(editor =>
                {
                    editor.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.View);
                    editor.AddDefaultButton(DefaultButtonType.Edit);
                    editor.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.Delete);

                    editor.AddField(x => x._Id)
                        .SetDescription("This should be readonly")
                        .SetReadonly();

                    editor.AddField(x => x.Name)
                        .SetDescription("This is a name");

                    editor.AddField(x => x.Description)
                        .SetDescription("This is a description")
                        .SetType(EditorType.TextArea);

                    editor.AddField(x => x.Number)
                        .SetDescription("This is a number")
                        .SetType(EditorType.Numeric);

                    editor.AddField(x => x.Image)
                        .SetDescription("This is an image");
                });

                listEditorConfig.AddSection<TestEntityVariantC>(editor =>
                {
                    editor.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.View);
                    editor.AddDefaultButton(DefaultButtonType.Edit);
                    editor.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                    editor.AddDefaultButton(DefaultButtonType.Delete);

                    editor.AddField(x => x._Id)
                        .SetDescription("This should be readonly")
                        .SetReadonly();

                    editor.AddField(x => x.Name)
                        .SetDescription("This is a name");

                    editor.AddField(x => x.Description)
                        .SetDescription("This is a description")
                        .SetType(EditorType.TextArea);

                    editor.AddField(x => x.Number)
                        .SetDescription("This is a number")
                        .SetType(EditorType.Numeric);

                    editor.AddField(x => x.Quote)
                        .SetDescription("This is a quote");
                });
            }

            void AzureTableStorageListView(ListViewConfig<AzureTableStorageEntity> config)
            {
                config
                    .AddDefaultButton(DefaultButtonType.New, isPrimary: true)
                    .AddCustomButton<CreateButtonActionHandler>(typeof(CreateButton), "Custom create!");

                config.AddRow(listPaneConfig =>
                {
                    listPaneConfig.AddField(x => x.Id);
                    listPaneConfig.AddField(x => x.Title);
                    listPaneConfig.AddField(x => x.Description);
                    listPaneConfig.AddField(x => x.Password);
                    listPaneConfig.AddField(x => x.Destroy == true ? "True" : x.Destroy == false ? "False" : "Null")
                        .SetName("Destroy");

                    listPaneConfig.AddDefaultButton(DefaultButtonType.Edit, isPrimary: true);
                    listPaneConfig.AddDefaultButton(DefaultButtonType.Delete);
                });
            }

            void AzureTableStorageEditor(NodeEditorConfig<AzureTableStorageEntity> config)
            {
                config.AddDefaultButton(DefaultButtonType.SaveExisting, isPrimary: true);
                config.AddDefaultButton(DefaultButtonType.SaveNew, isPrimary: true);
                config.AddDefaultButton(DefaultButtonType.Delete);

                config.AddSection(typeof(BlockSection));

                config.AddSection(editorPaneConfig =>
                {
                    editorPaneConfig.AddField(x => x.Title);
                    editorPaneConfig.AddField(x => x.Description);
                    editorPaneConfig.AddField(x => x.Password)
                        .SetType(typeof(PasswordEditor));
                    editorPaneConfig.AddField(x => x.Destroy);
                });
            }

            #endregion
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRapidCMS(isDevelopment: env.IsDevelopment());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            //app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}