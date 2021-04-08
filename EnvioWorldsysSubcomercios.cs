// <copyright file="EnvioWorldsysSubcomercios.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace EnvioWorldsysSubcomercios
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Dynamic;
    using System.Runtime.ExceptionServices;
    using Autofac;
    using BatchProcess;
    using BatchProcess.Models;
    using global::EnvioWorldsysSubcomercios.Models;
    using global::EnvioWorldsysSubcomercios.Stores;
    using Gp.Core;
    using Gp.Core.EventBus.Dummy;
    using Gp.Db.Entidad;
    using Gp.Db.Entidad.Adquirencia.Models;
    using Gp.Db.Entidad.Manager;
    using Gp.Db.Generics;
    using INFINITUS.Client;
    using INFINITUS.Client.Models;


    /// <summary>
    /// clase para EnvioWorldsysSubcomercios.
    /// </summary>
    public class EnvioWorldsysSubcomercios : BatchProcess<SubComercios>
    {
        private IDbTransactionProvider transactionProvider;
        private IRepository<SubComercios> subComerciosRepository;
        private Parametros parametros = new Parametros();
        private IINFINITUSClient clienINFINITUS;

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="builder">containerBuilder.</param>
        /// <returns>bool.</returns>
        protected override bool Initialize(ContainerBuilder builder)
        {
            var errores = this.parametros.ParseParametros(this.argumentos.Parametros);
            if (errores.Count > 0)
            {
                foreach (var error in errores)
                {
                    this.AddDetail("Error parseando parametros", EstadoDetalle.Error);
                }

                return false;
            }

            errores = this.parametros.ValidarParametros();
            if (errores.Count > 0)
            {
                foreach (var error in errores)
                {
                    this.AddDetail("Error validando parametros", EstadoDetalle.Error);
                }

                return false;
            }

            this.clienINFINITUS = RestClientStore.InfinitusClient(this.argumentos.Apar);

            this.ConfigurarAutoFac(builder);

            return true;
        }

        /// <summary>
        /// BeginProcess.
        /// </summary>
        /// <returns>bool.</returns>
        protected override bool BeginProcess()
        {
            return true;
        }

        /// <summary>
        /// ListObjects.
        /// </summary>
        /// <returns>listado de T2020DDetail.</returns>
        protected override IEnumerable<SubComercios> ListObjects()
        {
            return (from s in this.subComerciosRepository.Get()
                    where s.FechaUltimaModif == this.parametros.Fecha
                    select s).ToList();
        }

        /// <summary>
        /// DescribeObject.
        /// </summary>
        /// <param name="obj">T3000DetailMarca.</param>
        /// <param name="index">long.</param>
        /// <returns>string.</returns>
        protected override string DescribeObject(SubComercios obj, long index)
        {
            return null;
        }

        /// <summary>
        /// ProcessObject.
        /// </summary>
        /// <param name="details">T3000DetailMarca.</param>
        /// <returns>bool.</returns>
        protected override bool ProcessObject(SubComercios details)
        {
            try
            {
                this.clienINFINITUS.LavadoClientes.Post(
                    new LavadoClientesDTO()
                    {
                        FechaInformacion = DateTime.Now.Date,
                        Empresa = "0050",
                        IdentTributariaTipo = "11",
                        IdentTributariaNumero = details.DocumentoTitular,
                        Denominacion = $"{details.ApellidoTitular} {details.NombreTitular}",
                        FechaModificado = details.FechaUltimaModif,
                    },
                    RestClientStore.Token);
            }
            catch (Exception e)
            {
                throw;
            }
            return true;
        }

        /// <summary>
        /// Finishing.
        /// </summary>
        /// <returns>bool.</returns>
        protected override bool Finishing()
        {
            return true;
        }

        /// <summary>
        /// ResolveDependencies.
        /// </summary>
        /// <param name="scope">scope.</param>
        protected override void ResolveDependencies(ILifetimeScope scope)
        {
            this.transactionProvider = scope.Resolve<IDbTransactionProvider>();
            this.subComerciosRepository = scope.Resolve<IRepository<SubComercios>>();
        }

        private void ConfigurarAutoFac(ContainerBuilder builder)
        {
            var appContext = new ApplicationContext()
            {
                ProviderName = "Oracle",
                ConnectionString = this.argumentos.StringConexion,
                IdEntidad = this.parametros.IdEntidad,
                IdComponente = "PRBATCH",
                IdEjecucion = this.argumentos.RunId,
                Operacion = null,
                Origen = Origenes.ProcesoBatch,
            };

            builder.RegisterInstance(appContext).AsImplementedInterfaces();

            builder.RegisterModule(new Gp.Db.Generics.Module());

            builder.RegisterModule(new Gp.Db.Entidad.Module());

            builder.RegisterModule(new Gp.Db.Entidad.Adquirencia.Module());

            builder.RegisterType<EventBusDummy>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
