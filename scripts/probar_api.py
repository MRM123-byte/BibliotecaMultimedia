"""Pruebas HTTP reales contra SQL Server. Requiere build, migración y --seed previos.
Ejecutar: python scripts/probar_api.py. Usa solo la biblioteca estándar de Python.
Inicia su propia API en 5199, la reinicia y conserva una ficha de evidencia.
"""
import json, os, subprocess, time, uuid
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError
from datetime import datetime, timezone

ROOT=Path(__file__).resolve().parents[1]
APP=ROOT/'src/BibliotecaMultimedia.Api'
OUT=ROOT/'artifacts/evidencias'
OUT.mkdir(parents=True,exist_ok=True)
BASE='http://localhost:5199'
results=[]
password=os.environ.get('Seed__Password')
if not password: raise SystemExit('Define Seed__Password con la contraseña utilizada al cargar los datos de prueba.')
suffix=uuid.uuid4().hex[:10]

def request(method,path,data=None,token=None):
    headers={'Content-Type':'application/json'}
    if token:headers['Authorization']='Bearer '+token
    req=Request(BASE+path,data=json.dumps(data).encode() if data is not None else None,headers=headers,method=method)
    try: response=urlopen(req,timeout=20)
    except HTTPError as e:response=e
    raw=response.read().decode()
    try:body=json.loads(raw) if raw else None
    except ValueError:body=raw
    return response.status,dict(response.headers),body

def check(label,method,path,expected,data=None,token=None):
    status,headers,body=request(method,path,data,token)
    # Nunca escribir tokens ni contraseñas en el informe.
    safe_body={'autenticado':True} if path.startswith('/api/auth/login') and status==200 else body
    results.append({'prueba':label,'operacion':method+' '+path,'esperado':expected,'http':status,
        'location':headers.get('Location'),'respuesta':safe_body})
    assert status==expected, f'{label}: esperado {expected}, recibido {status}: {body}'
    print(f'{status} {label}')
    return body,headers

def start():
    env=os.environ.copy();env['ASPNETCORE_ENVIRONMENT']='Development';env['ASPNETCORE_URLS']=BASE
    log=open(OUT/'servidor-pruebas.log','a',encoding='utf8')
    proc=subprocess.Popen(['dotnet',str(APP/'bin/Debug/net10.0/BibliotecaMultimedia.Api.dll')],cwd=APP,env=env,stdout=log,stderr=log)
    log.close()
    for _ in range(80):
        if proc.poll() is not None:raise RuntimeError('La API terminó antes de iniciar. Revisar servidor-pruebas.log.')
        try:
            if request('GET','/api/generos')[0]==200:return proc
        except (URLError,ConnectionError,TimeoutError):pass
        time.sleep(.25)
    proc.terminate();proc.wait(timeout=10)
    raise RuntimeError('No inició la API de pruebas.')

proc=None
try:
    proc=start();pid_before=proc.pid
    admin=request('POST','/api/auth/login?useCookies=false',{'email':'catalogador@example.test','password':password})
    assert admin[0]==200,'No se pudo autenticar Catalogador. Revisa --seed y Seed__Password.'
    token=admin[2]['accessToken']
    reader=request('POST','/api/auth/login?useCookies=false',{'email':'lector@example.test','password':password})
    assert reader[0]==200
    other=reader[2]['accessToken']
    check('Swagger OpenAPI disponible','GET','/swagger/v1/swagger.json',200)
    # El JSON de Swagger se comprueba pero no se copia completo a las evidencias.
    results[-1]['respuesta']={'documentacion':'Disponible','rutas':len(results[-1]['respuesta']['paths'])}
    check('Registro de usuario','POST','/api/auth/register',200,{'email':f'prueba-{suffix}@example.test','password':password})
    check('Biblioteca sin sesión','GET','/api/biblioteca',401)
    check('Paginación excesiva','GET','/api/contenidos?pagina=2147483647',400)
    check('Perfil propio','GET','/api/usuarios/me',200,token=token)
    genre,_=check('Crear género','POST','/api/generos',201,{'codigo':'PRUEBA_'+suffix.upper(),'nombre':'Género de prueba'},token)
    gid=genre['generoId']
    check('Consultar género','GET',f'/api/generos/{gid}',200)
    check('Actualizar género','PUT',f'/api/generos/{gid}',204,{'codigo':genre['codigo'],'nombre':'Género actualizado'},token)
    content={'titulo':'Obra de evidencia '+suffix,'tipoContenido':'Pelicula','esAnime':False,
        'descripcion':'Ficha ficticia creada mediante POST para verificar persistencia.','generoIds':[gid],
        'referencia':{'proveedor':'TMDB','tipoRecurso':'movie','idExterno':'prueba-'+suffix}}
    check('Título obligatorio','POST','/api/contenidos',400,{**content,'titulo':''},token)
    check('Referencia de género inválida','POST','/api/contenidos',400,{**content,'generoIds':[2147483647]},token)
    check('Regla de anime en videojuegos','POST','/api/contenidos',400,{**content,'tipoContenido':'Videojuego','esAnime':True},token)
    check('Usuario lector no modifica catálogo','POST','/api/contenidos',403,content,other)
    created,headers=check('Crear contenido y Location','POST','/api/contenidos',201,content,token)
    cid=created['contenidoId'];assert headers.get('Location','').endswith('/api/contenidos/'+str(cid))
    check('Referencia externa duplicada','POST','/api/contenidos',400,content,token)
    check('Listado de contenidos','GET','/api/contenidos',200)
    check('Consulta por ID','GET',f'/api/contenidos/{cid}',200)
    check('Consulta de géneros relacionados','GET',f'/api/contenidos/{cid}/generos',200)
    check('Consulta de referencia externa','GET',f'/api/contenidos/{cid}/referencias',200)
    content['titulo']='Obra persistente '+suffix
    check('Actualizar contenido','PUT',f'/api/contenidos/{cid}',204,content,token)
    updated,_=check('Verificar actualización','GET',f'/api/contenidos/{cid}',200)
    assert updated['titulo']==content['titulo']
    check('Contenido inexistente','GET','/api/contenidos/2147483647',404)
    check('Actualizar inexistente','PUT','/api/contenidos/2147483647',404,content,token)
    check('Contenido inválido en biblioteca','POST','/api/biblioteca',400,{'contenidoId':2147483647},token)
    entry,_=check('Añadir a biblioteca','POST','/api/biblioteca',201,{'contenidoId':cid},token)
    eid=entry['entradaBibliotecaId'];assert entry['estado']=='Pendiente'
    check('Evitar entrada duplicada','POST','/api/biblioteca',400,{'contenidoId':cid},token)
    check('Proteger ficha compartida','DELETE',f'/api/contenidos/{cid}',400,token=token)
    check('Proteger género utilizado','DELETE',f'/api/generos/{gid}',400,token=token)
    check('No revelar entrada ajena','GET',f'/api/biblioteca/{eid}',404,token=other)
    check('No modificar entrada ajena','PUT',f'/api/biblioteca/{eid}',404,{'estado':'Completado','puntuacion':9},other)
    check('Puntuación fuera de rango','PUT',f'/api/biblioteca/{eid}',400,{'estado':'EnCurso','puntuacion':11},token)
    check('Estado inválido','PUT',f'/api/biblioteca/{eid}',400,{'estado':'Desconocido'},token)
    check('Puntuar sin completar','PUT',f'/api/biblioteca/{eid}',204,{'estado':'EnCurso','puntuacion':8,'resena':'Prueba de persistencia.'},token)
    watch,_=check('Watchlist excluye EnCurso','GET','/api/biblioteca?estado=Pendiente',200,token=token)
    assert all(x['entradaBibliotecaId']!=eid for x in watch['elementos'])
    check('Retirar puntuación y reseña','PUT',f'/api/biblioteca/{eid}',204,{'estado':'EnCurso','puntuacion':None,'resena':None},token)
    cleared,_=check('Comprobar valores nulos','GET',f'/api/biblioteca/{eid}',200,token=token)
    assert cleared['puntuacion'] is None and cleared['resena'] is None
    # Detener solo el proceso iniciado por esta prueba, nunca el servidor SQL.
    proc.terminate();proc.wait(timeout=15);proc=None
    time.sleep(.5)
    proc=start();pid_after=proc.pid
    persisted,_=check('GET después de reiniciar la API','GET',f'/api/contenidos/{cid}',200)
    assert persisted==updated,'Los datos cambiaron tras el reinicio.'
    check('Biblioteca persistente tras reiniciar','GET',f'/api/biblioteca/{eid}',200,token=token)
    check('Retirar entrada personal','DELETE',f'/api/biblioteca/{eid}',204,token=token)
    check('Entrada retirada no existe','GET',f'/api/biblioteca/{eid}',404,token=token)
    check('Ficha compartida conservada','GET',f'/api/contenidos/{cid}',200)
    disposable={**content,'titulo':'Ficha desechable','generoIds':[],
        'referencia':{'proveedor':'RAWG','tipoRecurso':'game','idExterno':'borrar-'+suffix},'tipoContenido':'Videojuego','esAnime':False}
    temp,_=check('Crear ficha para DELETE','POST','/api/contenidos',201,disposable,token)
    check('Eliminar contenido','DELETE',f"/api/contenidos/{temp['contenidoId']}",204,token=token)
    check('Contenido eliminado no existe','GET',f"/api/contenidos/{temp['contenidoId']}",404)
    tempg,_=check('Crear género sin relaciones','POST','/api/generos',201,{'codigo':'BORRAR_'+suffix.upper(),'nombre':'Temporal'},token)
    check('Eliminar género','DELETE',f"/api/generos/{tempg['generoId']}",204,token=token)
    report={'fechaUtc':datetime.now(timezone.utc).isoformat(),'baseUrl':BASE,'resultado':'APROBADO',
        'reinicio':{'pidAntes':pid_before,'pidDespues':pid_after,'contenidoId':cid,'titulo':content['titulo']},'pruebas':results}
    (OUT/'resultados.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    print(f'APROBADO: {len(results)} verificaciones. Evidencias: {OUT / "resultados.json"}')
finally:
    if proc is not None and proc.poll() is None:proc.terminate();proc.wait(timeout=15)
