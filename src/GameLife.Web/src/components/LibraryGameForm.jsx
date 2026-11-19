import { useState } from 'react';
import LibrarySubscriptionFields from './LibrarySubscriptionFields.jsx';
import LibraryPlatformAccess from './LibraryPlatformAccess.jsx';
import {
  SERVICOS_ASSINATURA,
  SITUACOES_ASSINATURA,
  excluirAssinatura,
  obterAssinaturas
} from '../api/assinaturas.js';

const estadoPlataforma = () => ({
  compraDigital: true,
  midiaFisica: false,
  porAssinatura: false,
  resgatadoDeAssinatura: false,
  servicoExigido: ''
});

const valorCampo = (valor) => valor == null ? '' : String(valor);
const paraInputDate = (valor) => (valor ? String(valor).slice(0, 10) : '');
const chavePlataforma = (valor) => valor.trim().normalize('NFKC').toLocaleUpperCase('pt-BR');

const criarOpcoesPlataforma = (platforms, jogo) => {
  const opcoes = [...platforms];
  for (const plataforma of jogo?.plataformas ?? []) {
    if (!opcoes.some((opcao) => chavePlataforma(opcao) === chavePlataforma(plataforma.nome))) {
      opcoes.push(plataforma.nome);
    }
  }
  return opcoes;
};

const criarEstadoInicial = (jogo, opcoes) => Object.fromEntries((jogo?.plataformas ?? []).map((plataforma) => {
  const nomeOpcao = opcoes.find((opcao) => chavePlataforma(opcao) === chavePlataforma(plataforma.nome)) ?? plataforma.nome;
  const midiaFisica = Boolean(plataforma.midiaFisica);
  const porAssinatura = Boolean(plataforma.porAssinatura);
  const compraDigital = plataforma.compraDigital ?? (!midiaFisica && !porAssinatura);
  return [nomeOpcao, {
    compraDigital,
    midiaFisica,
    porAssinatura: porAssinatura && !plataforma.resgatadoDeAssinatura,
    resgatadoDeAssinatura: Boolean(plataforma.resgatadoDeAssinatura),
    resgateOriginal: Boolean(plataforma.resgatadoDeAssinatura),
    servicoExigido: plataforma.servicoExigido ?? ''
  }];
}));

const criarRascunhoAssinatura = (assinatura = null) => ({
  id: assinatura?.id ?? null,
  servico: assinatura?.servico ?? SERVICOS_ASSINATURA[0],
  situacao: assinatura?.situacao ?? SITUACOES_ASSINATURA[1],
  dataDisponibilidade: paraInputDate(assinatura?.dataDisponibilidade),
  dataEncerramento: paraInputDate(assinatura?.dataEncerramento)
});

function LibraryGameForm({ platforms, saving, onSubmit, initialGame = null, onCancel }) {
  const opcoesPlataforma = criarOpcoesPlataforma(platforms, initialGame);
  const [titulo, setTitulo] = useState(() => initialGame?.titulo ?? '');
  const [anoLancamento, setAnoLancamento] = useState(() => valorCampo(initialGame?.anoLancamento));
  const [selecionadas, setSelecionadas] = useState(() => criarEstadoInicial(initialGame, opcoesPlataforma));
  const [assinaturas, setAssinaturas] = useState(() => (initialGame?.assinaturas ?? []).map((item) => ({ ...item })));
  const [assinaturasRemovidas, setAssinaturasRemovidas] = useState([]);
  const [editorAssinatura, setEditorAssinatura] = useState(null);
  const [assinaturasEditadasPeloUsuario, setAssinaturasEditadasPeloUsuario] = useState(false);
  const [erro, setErro] = useState('');

  const editando = Boolean(initialGame);
  const plataformasOriginais = new Set((initialGame?.plataformas ?? []).map((plataforma) => chavePlataforma(plataforma.nome)));
  const temAlgumaAssinaturaMarcada = Object.values(selecionadas).some((dados) => dados.porAssinatura && !dados.resgatadoDeAssinatura);
  const exibirSecaoAssinaturas = temAlgumaAssinaturaMarcada || assinaturas.length > 0 || editorAssinatura !== null;

  const carregarAssinaturasCompartilhadas = async () => {
    if (editando || assinaturasEditadasPeloUsuario) return;
    const tituloLimpo = titulo.trim();
    if (!tituloLimpo) return;
    try {
      const lista = await obterAssinaturas({ titulo: tituloLimpo });
      if (Array.isArray(lista) && lista.length > 0) {
        setAssinaturas(lista);
      }
    } catch {
      // Consulta opcional de compartilhamento; falha silenciosa não bloqueia cadastro.
    }
  };

  const alternarPlataforma = (plataforma, marcada) => {
    setSelecionadas((atuais) => {
      if (marcada) return { ...atuais, [plataforma]: estadoPlataforma() };
      const proximas = { ...atuais };
      delete proximas[plataforma];
      return proximas;
    });
    setErro('');
  };

  const atualizarPlataforma = (plataforma, campo, valor) => {
    setSelecionadas((atuais) => ({
      ...atuais,
      [plataforma]: {
        ...atuais[plataforma],
        [campo]: valor,
        ...(campo === 'resgatadoDeAssinatura' && valor ? {
          servicoExigido: atuais[plataforma].servicoExigido || SERVICOS_ASSINATURA[0]
        } : {})
      }
    }));
    if (campo === 'porAssinatura' && valor && !selecionadas[plataforma]?.resgatadoDeAssinatura && assinaturas.length === 0 && !editorAssinatura) {
      setEditorAssinatura(criarRascunhoAssinatura());
    }
    setErro('');
  };

  const confirmarRascunhoAssinatura = () => {
    if (!editorAssinatura) return null;
    if (
      editorAssinatura.dataDisponibilidade &&
      editorAssinatura.dataEncerramento &&
      editorAssinatura.dataEncerramento < editorAssinatura.dataDisponibilidade
    ) {
      setErro('A data de encerramento da assinatura não pode ser anterior à data de entrada.');
      return false;
    }

    const nova = {
      id: editorAssinatura.id,
      servico: editorAssinatura.servico,
      situacao: editorAssinatura.situacao,
      dataDisponibilidade: editorAssinatura.dataDisponibilidade || null,
      dataEncerramento: editorAssinatura.dataEncerramento || null
    };

    const atualizadas = [
      ...assinaturas.filter((item) => item.servico !== nova.servico),
      nova
    ].sort((a, b) => a.servico.localeCompare(b.servico, 'pt-BR'));

    setAssinaturas(atualizadas);
    setAssinaturasEditadasPeloUsuario(true);
    setEditorAssinatura(null);
    setErro('');
    return atualizadas;
  };

  const removerAssinaturaDaLista = (assinatura) => {
    setAssinaturas((atuais) => atuais.filter((item) => item.servico !== assinatura.servico));
    if (assinatura.id) {
      setAssinaturasRemovidas((atuais) => [...atuais, assinatura.id]);
    }
    setAssinaturasEditadasPeloUsuario(true);
    setErro('');
  };

  const validar = (assinaturasEfetivas) => {
    if (!titulo.trim()) return 'Informe o nome do jogo.';
    if (Object.keys(selecionadas).length === 0) return 'Selecione pelo menos uma plataforma.';
    if (anoLancamento !== '' && (Number(anoLancamento) < 1 || Number(anoLancamento) > 9999)) {
      return 'Informe um ano de lançamento válido.';
    }

    for (const [plataforma, dados] of Object.entries(selecionadas)) {
      if (!dados.compraDigital && !dados.midiaFisica && !dados.porAssinatura && !dados.resgatadoDeAssinatura) {
        return `Selecione pelo menos uma forma de acesso para ${plataforma}.`;
      }
      if (dados.resgatadoDeAssinatura && !dados.servicoExigido) {
        return `Informe o serviço de assinatura exigido em ${plataforma}.`;
      }

    }

    if (temAlgumaAssinaturaMarcada && assinaturasEfetivas.length === 0) {
      return 'Informe pelo menos uma assinatura ou desmarque a opção Assinatura.';
    }

    for (const item of assinaturasEfetivas) {
      if (item.dataDisponibilidade && item.dataEncerramento && item.dataEncerramento < item.dataDisponibilidade) {
        return 'A data de encerramento da assinatura não pode ser anterior à data de entrada.';
      }
    }

    return '';
  };

  const enviar = async (evento) => {
    evento.preventDefault();
    let assinaturasEfetivas = assinaturas;
    if (editorAssinatura) {
      const resultadoRascunho = confirmarRascunhoAssinatura();
      if (resultadoRascunho === false) return;
      assinaturasEfetivas = resultadoRascunho;
    }

    const mensagemErro = validar(assinaturasEfetivas);
    if (mensagemErro) {
      setErro(mensagemErro);
      return;
    }

    const payload = {
      titulo: titulo.trim(),
      ...(anoLancamento === '' ? {} : { anoLancamento: Number(anoLancamento) }),
      plataformas: opcoesPlataforma
        .filter((plataforma) => selecionadas[plataforma])
        .map((plataforma) => {
          const dados = selecionadas[plataforma];
          return {
            plataforma,
            compraDigital: dados.compraDigital,
            midiaFisica: dados.midiaFisica,
            porAssinatura: dados.porAssinatura || dados.resgatadoDeAssinatura,
            ...(dados.resgatadoDeAssinatura
              ? { resgatadoDeAssinatura: true, servicoExigido: dados.servicoExigido }
              : dados.resgateOriginal ? { resgatadoDeAssinatura: false } : {})
          };
        }),
      ...(assinaturasEfetivas.length > 0
        ? {
            assinaturas: assinaturasEfetivas.map((item) => ({
              servico: item.servico,
              situacao: item.situacao,
              dataDisponibilidade: item.dataDisponibilidade ? String(item.dataDisponibilidade).slice(0, 10) : null,
              dataEncerramento: item.dataEncerramento ? String(item.dataEncerramento).slice(0, 10) : null
            }))
          }
        : {})
    };

    try {
      await onSubmit(payload);
      if (assinaturasRemovidas.length > 0) {
        await Promise.allSettled(assinaturasRemovidas.map((id) => excluirAssinatura(id)));
      }
    } catch {
      return;
    }

    if (!editando) {
      setTitulo('');
      setAnoLancamento('');
      setSelecionadas({});
      setAssinaturas([]);
      setAssinaturasRemovidas([]);
      setEditorAssinatura(null);
      setAssinaturasEditadasPeloUsuario(false);
    }
    setErro('');
  };

  return (
    <form className="library-game-form" onSubmit={enviar}>
      <div className="library-game-main-fields">
        <label htmlFor="titulo">
          Nome do jogo
          <input
            id="titulo"
            value={titulo}
            onChange={(evento) => setTitulo(evento.target.value)}
            onBlur={carregarAssinaturasCompartilhadas}
            maxLength="200"
            placeholder="Ex.: Graveyard Keeper"
            autoComplete="off"
            disabled={saving || editando}
          />
        </label>
        <label htmlFor="ano-lancamento">
          Ano de lançamento (opcional)
          <input
            id="ano-lancamento"
            type="number"
            min="1"
            max="9999"
            value={anoLancamento}
            onChange={(evento) => setAnoLancamento(evento.target.value)}
            disabled={saving}
          />
        </label>
      </div>

      <fieldset className="platform-selector">
        <legend>Plataformas</legend>
        <p>{editando
          ? 'As plataformas já cadastradas estão preservadas. Marque também os outros lugares onde você acessa o jogo.'
          : 'Selecione todas as plataformas onde você acessa ou possui o jogo.'}</p>
        <div className="platform-options">
          {opcoesPlataforma.map((plataforma) => (
            <label key={plataforma} className={selecionadas[plataforma] ? 'selected' : ''}>
              <input
                type="checkbox"
                aria-label={`Selecionar ${plataforma}`}
                checked={Boolean(selecionadas[plataforma])}
                onChange={(evento) => alternarPlataforma(plataforma, evento.target.checked)}
                disabled={saving || plataformasOriginais.has(chavePlataforma(plataforma))}
              />
              <span>{plataforma}</span>
            </label>
          ))}
        </div>
      </fieldset>

      <div className="selected-platforms">
        {Object.entries(selecionadas).map(([plataforma, dados]) => (
          <LibraryPlatformAccess key={plataforma} plataforma={plataforma} dados={dados}
            atualizarPlataforma={atualizarPlataforma} saving={saving} />
        ))}
      </div>

      {exibirSecaoAssinaturas && (
        <LibrarySubscriptionFields
          assinaturas={assinaturas}
          editorAssinatura={editorAssinatura}
          setEditorAssinatura={setEditorAssinatura}
          iniciarEdicao={(assinatura) => setEditorAssinatura(criarRascunhoAssinatura(assinatura))}
          confirmarRascunhoAssinatura={confirmarRascunhoAssinatura}
          removerAssinaturaDaLista={removerAssinaturaDaLista}
          saving={saving}
        />
      )}

      {erro && <div className="feedback erro" role="alert">{erro}</div>}

      <div className="library-form-actions">
        {editando && <button type="button" className="btn-ghost" onClick={onCancel} disabled={saving}>Cancelar</button>}
        <button className="btn-primary" type="submit" disabled={saving}>
          {saving ? (editando ? 'Salvando...' : 'Adicionando...') : (editando ? 'Salvar alterações' : 'Adicionar à biblioteca')}
        </button>
      </div>
    </form>
  );
}

export default LibraryGameForm;
